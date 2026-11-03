# FCM Notifications

## Existing architecture

- ASP.NET Core targets .NET 8 and uses EF Core 8 with PostgreSQL/Npgsql.
- Product requests are stored in the existing `ProductRequests` table. `UserId` is nullable.
- Notifications use the existing `Notifications` table. User history remains scoped by the authenticated user's ID; global broadcast rows have no user recipient.
- JWT bearer validation is wired in `Program.cs`, but there is no active authentication controller in this project. `AuthController.cs.bak` is not compiled and must not be re-enabled as-is: it accepts plaintext passwords and has no secure admin provisioning flow.
- Product-request moderation routes were previously anonymous. They now require an Admin JWT. Notification and device-token APIs require a User or Admin JWT.
- Product creation and category create/update/delete now require an Admin JWT; catalog reads and sales routes are unchanged.
- Product approval currently changes request status only. A request has no catalog price or category; its requested quantity is demand, not verified stock. Therefore approval does not create an inventory item. Add an Admin-supplied catalog DTO for price, category, and starting stock before adding that behavior.

## Database changes

The `AddFcmNotificationOutbox` migration adds:

- `DeviceTokens`, linked to `Users`, with a unique SHA-256 token hash and active state.
- `NotificationOutboxMessages`, one delivery job per notification/device token, with retries and invalid-token cancellation.
- Nullable unique `ProductRequests.IdempotencyKey` and `Notifications.EventKey` columns.

The `AddFestivalSchedules` migration adds `FestivalSchedules` for Admin-managed Maharashtra festival names/dates. Festival dates are stored as PostgreSQL `date` values.

The `AnonymousFcmBroadcast` migration makes notification recipients optional and permits outbox rows to target an FCM topic instead of a stored device token.

The existing `Notifications` and `ProductRequests` tables are reused. The API currently runs `Database.Migrate()` on startup, so Render applies the migration during deployment. To apply locally, run `dotnet ef database update` with a development PostgreSQL connection.

## API contract

All protected routes require `Authorization: Bearer <JWT>`. User identity is read from the validated JWT `NameIdentifier`/`sub` claim; request bodies cannot choose a recipient or role.

| Method | Route | Authorization | Purpose |
| --- | --- | --- | --- |
| `POST` | `/api/device-tokens/register` | `User` or `Admin` | Register/refresh the caller's FCM token |
| `DELETE` | `/api/device-tokens/unregister` | `User` or `Admin` | Deactivate the caller's FCM token |
| `GET` | `/api/notification/my` | `User` or `Admin` | Get only the caller's notification history |
| `PUT` | `/api/notification/read/{id}` | `User` or `Admin` | Mark only the caller's notification as read |
| `POST` | `/api/ProductRequest` | Anonymous; authenticated identity is used if present | Save a request and broadcast a generic announcement |
| `GET` | `/api/ProductRequest/admin/requests` | `Admin` | List pending requests |
| `GET` | `/api/ProductRequest/admin/pending` | `Admin` | List pending requests (existing alias) |
| `GET` | `/api/ProductRequest/admin/history` | `Admin` | List processed requests |
| `PUT` | `/api/ProductRequest/approve/{id}` | `Admin` | Approve a pending request and notify only its associated user |
| `PUT` | `/api/ProductRequest/reject/{id}` | `Admin` | Reject a pending request and notify only its associated user |
| `GET` | `/api/festivals` | `Admin` | List configured festival dates |
| `GET` | `/api/festivals/{id}` | `Admin` | Get a configured festival |
| `POST` | `/api/festivals` | `Admin` | Add a festival name/date |
| `PUT` | `/api/festivals/{id}` | `Admin` | Update a festival name/date |
| `DELETE` | `/api/festivals/{id}` | `Admin` | Deactivate a festival schedule |

### Register or refresh a device token

Request:

```http
POST /api/device-tokens/register
Authorization: Bearer <user-jwt>
Content-Type: application/json
```

```json
{
  "token": "<FCM registration token>"
}
```

Response `200`:

```json
{
  "message": "FCM token registered."
}
```

Invalid length/format returns `400`; missing/invalid authentication or a user ID absent from `Users` returns `401`. A token is unique across users; registering a refreshed/shared token attaches it to the currently authenticated user. The response never returns the stored token.

### Unregister a device token

Request:

```http
DELETE /api/device-tokens/unregister
Authorization: Bearer <user-jwt>
Content-Type: application/json
```

```json
{
  "token": "<FCM registration token>"
}
```

Response `204` whether the caller's token existed or was already inactive. A caller cannot deactivate another user's token.

### Read and mark notifications

`GET /api/notification/my` returns `200` with an array of the caller's notifications, newest first. Each row includes `id`, `userId`, `message`, `isRead`, `createdAt`, and `eventKey`.

`PUT /api/notification/read/{id}` returns `200` with `{"message":"Notification marked as read."}` for the caller's notification, or `404` when it does not exist or belongs to another user.

Unauthenticated calls return `401`; an authenticated role outside `User`/`Admin` returns `403`.

### Product request events

Request:

```http
POST /api/ProductRequest
Content-Type: application/json
Idempotency-Key: <stable-unique-request-key>
```

```json
{
  "productName": "Amul Cheese",
  "description": "200 g pack",
  "requestedBy": 1,
  "quantity": 2
}
```

`userId` and `role` in a body are ignored. If a valid User/Admin JWT is supplied, the request owner is taken from its user-ID claim; an anonymous request has no linked customer and cannot receive customer-specific status notifications. A successful response is `200` with a `requestId` and `status`. Invalid name/quantity or malformed idempotency key returns `400`.

Use the same `Idempotency-Key` for retries. Replays return the original request ID/status and do not create another request or notification. The header remains optional for backward compatibility; without it, the server cannot distinguish a retried anonymous request from a separate identical request.

Approving/rejecting a pending request returns `200`. Requests already processed return `409`; nonexistent IDs return `404`. In temporary broadcast mode, the generic messages `A product request for <product> has been accepted.` or `A product request for <product> was rejected.` are delivered to every device subscribed to the global announcement topic. New requests are broadcast the same way. These messages do not identify the customer. Request moderation still requires an Admin JWT.

No public Admin notification/test endpoint is provided.

### Maharashtra festival reminders

There is no festival calendar in the existing project, so dates are not guessed or automatically seeded. An Admin must schedule the verified date for each festival/year using:

```http
POST /api/festivals
Authorization: Bearer <admin-jwt>
Content-Type: application/json
```

```json
{
  "name": "Ganesh Chaturthi",
  "festivalDate": "YYYY-MM-DD"
}
```

The API returns `201` with the created record; invalid/past dates return `400`, and a duplicate name/date returns `409`. Use `GET /api/festivals` to review entries, `PUT /api/festivals/{id}` to correct one, and `DELETE /api/festivals/{id}` to deactivate it (`204`). All schedule-management endpoints require an Admin JWT.

`FestivalReminderWorker` checks every 15 minutes using `Asia/Kolkata`. On the local calendar date exactly seven days before a scheduled festival, it queues one notification to the global announcement topic. Event keys prevent repeated notification rows if the worker polls again. Schedule dates must be entered before the seven-day reminder date; there are no built-in Maharashtra holiday dates.

### Temporary no-login broadcast mode

Flutter installs that should receive global pushes must subscribe to the FCM topic `kirana_store_announcements` after Firebase initialization:

```dart
await FirebaseMessaging.instance.subscribeToTopic('kirana_store_announcements');
```

Topic subscription does not require the backend login or device-token APIs. It is intentionally public: every subscribed install receives each new-request, approval/rejection, and festival event. Do not put customer names, phone numbers, request owner data, or other private details in broadcast messages. The Admin moderation APIs remain protected; no login bypass was added. To switch back to private delivery later, remove the topic subscription and use authenticated device-token registration plus user-linked requests.

Broadcast notification rows have no individual user recipient. They appear as FCM pushes on subscribed devices but are not returned by `GET /api/notification/my` and cannot be individually marked read through the current user-scoped API. Per-device in-app history requires a later authenticated user flow or a separate installation identity/history design.

## Firebase and Render

The Flutter Firebase configuration is client configuration, not a server credential. Never upload `google-services.json`, `GoogleService-Info.plist`, a service-account key, or a private key to Flutter, source control, or this repository.

1. In Firebase Console, select project `kirana-shop-app-be084`, open **Project settings > Service accounts**, and generate/download a Firebase Admin service-account key. Keep the downloaded file private; do not paste it into chat or commit it.
2. In Render, open the backend Web Service's **Environment** settings and add `FIREBASE_SERVICE_ACCOUNT_JSON` as a secret containing the complete service-account JSON. Alternatively add it as a Render Secret File and set `GOOGLE_APPLICATION_CREDENTIALS` to its absolute mounted path (Render Secret Files are mounted under `/etc/secrets/`).
3. Add `FIREBASE_PROJECT_ID=kirana-shop-app-be084` if you want to make the project explicit; this is not a credential.
4. Configure a newly rotated JWT signing key through Render as `JWT_SIGNING_KEY` (at least 32 bytes). Keep it separate from Firebase credentials and never use a development signing key in production. The old key is present in tracked `appsettings.json`; remove it from the file and rotate it before deployment. Assume it has been exposed and invalidate tokens signed with it.
5. Save changes and redeploy. Startup logs should say `Firebase Cloud Messaging initialized successfully.` If credentials are absent/invalid, startup logs a warning and the API remains available, but push delivery is disabled. Outbox jobs retry with backoff and resume after valid credentials are deployed; request/business writes still commit.
6. After deploying, check Render logs for Firebase initialization and PostgreSQL migration success. Register a device token, submit a product request, and verify both Admin notification history and the device push.

Use a service account with only the permissions needed to send Firebase Cloud Messaging messages. Rotate/revoke a key in Google Cloud if it is ever exposed. Never log the JSON, private key, or raw device token.

## Authentication limitation and safe testing

The backend validates JWTs but this project has no active, secure endpoint that issues them. Protected endpoints intentionally fail closed; do not add a temporary public route accepting `UserId`/`Role`, and do not enable `AuthController.cs.bak` as written.

For isolated development testing, seed test users with `User`/`Admin` roles through a trusted local database operation and use a separate development signing key with a trusted local JWT issuer/script to create short-lived tokens containing the seeded `NameIdentifier` and role claims. Keep this key and issuer off Render production. Before production use, implement a reviewed password-hash login/admin provisioning flow or validate Firebase Authentication ID tokens and map verified Firebase identities to database users. Use the resulting verified identity claims for all protected endpoints.

## Postman verification cases

Set `baseUrl`, `userToken`, `adminToken`, and `deviceToken` as local Postman variables. Obtain JWTs only through the trusted development process described above. Do not place service-account credentials or production JWT signing keys in Postman collections.

1. **Register token:** `POST {{baseUrl}}/api/device-tokens/register` with `Bearer {{userToken}}` and `{"token":"{{deviceToken}}"}` returns `200`. Repeat to verify refresh/upsert still has one token record.
2. **Invalid token:** send an empty, short, whitespace-containing, or oversized token; expect `400`.
3. **New request broadcast:** subscribe a test device to `kirana_store_announcements`, then submit `POST /api/ProductRequest` with an Idempotency-Key and body above. Expect `200` and one generic push to subscribed devices when FCM is configured.
4. **Accepted request broadcast:** call `PUT /api/ProductRequest/approve/{requestId}` with `Bearer {{adminToken}}`; expect `200` and a generic acceptance push on all subscribed devices.
5. **Rejected request broadcast:** create another request, then call `PUT /api/ProductRequest/reject/{requestId}` with `Bearer {{adminToken}}`; expect `200` and a generic rejection push on all subscribed devices.
6. **Expired/unregistered token:** register a real token, then revoke it in the device/FCM environment and trigger an event. The API business write still succeeds; worker logs show deactivation, and subsequent sends do not target the inactive token.
7. **Unauthorized moderation:** call `PUT /api/ProductRequest/approve/{id}` without a bearer token; expect `401`. With a valid User-role token, expect `403`.
8. **Cross-user history:** call `GET /api/notification/my` as user A and verify only A's records. Try `PUT /api/notification/read/{userBNotificationId}` as A; expect `404`.
9. **Duplicate prevention:** replay the same product-request POST with the same Idempotency-Key; expect the same `requestId` and only one notification event. Replaying a processed approval returns `409` and does not create another notification.
10. **Firebase unavailable/misconfigured:** remove the FCM secret or use an invalid secret in a non-production environment, then restart. API starts with a clear warning; product request/approval persistence remains functional; no push is sent. Restore the secret and redeploy to verify initialization.
11. **Festival reminder:** as Admin, `POST /api/festivals` with a verified date seven days ahead. On that date (within the 15-minute polling interval), each User with an active device token gets one notification/push; poll again and verify no duplicate notification appears.

Firebase delivery is at-least-once across process crashes: if Firebase accepts a push and the process dies before recording delivery, that outbox item may be retried. Database event keys prevent duplicate business notification rows and ordinary API retries; FCM does not provide an exactly-once delivery guarantee.

## Flutter integration

In temporary no-login mode, the Flutter app should initialize `firebase_core` and `firebase_messaging`, request notification permission where required, then call `FirebaseMessaging.instance.subscribeToTopic('kirana_store_announcements')`. It does not need to register an FCM token with the backend for topic broadcasts. For future private notifications, the authenticated flow remains available: register `getToken()` at `/api/device-tokens/register`, listen to `onTokenRefresh`, and unregister at `/api/device-tokens/unregister` on logout.

Handle `FirebaseMessaging.onMessage` for foreground notifications and configure a top-level background message handler for background delivery. FCM registration alone does not authenticate the user; token registration must wait until the app has a verified backend identity token.