using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;

namespace ShopAPI.Services
{
    public sealed class NotificationOutboxWorker : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan ProcessingLease = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IFcmPushSender _pushSender;
        private readonly ILogger<NotificationOutboxWorker> _logger;

        public NotificationOutboxWorker(
            IServiceScopeFactory scopeFactory,
            IFcmPushSender pushSender,
            ILogger<NotificationOutboxWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _pushSender = pushSender;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Notification outbox processing failed; the next poll will retry.");
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
        }

        private async Task ProcessBatchAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTime.UtcNow;
            var expiredLease = now - ProcessingLease;

            var dueMessageIds = await context.NotificationOutboxMessages
                .Where(message =>
                    message.DeliveredAt == null &&
                    message.CancelledAt == null &&
                    message.NextAttemptAt <= now &&
                    (message.ProcessingStartedAt == null ||
                     message.ProcessingStartedAt < expiredLease))
                .OrderBy(message => message.Id)
                .Select(message => message.Id)
                .Take(25)
                .ToListAsync(cancellationToken);

            foreach (var messageId in dueMessageIds)
            {
                await ProcessMessageAsync(
                    context,
                    messageId,
                    now,
                    expiredLease,
                    cancellationToken);
            }
        }

        private async Task ProcessMessageAsync(
            AppDbContext context,
            int messageId,
            DateTime now,
            DateTime expiredLease,
            CancellationToken cancellationToken)
        {
            var claimed = await context.NotificationOutboxMessages
                .Where(message =>
                    message.Id == messageId &&
                    message.DeliveredAt == null &&
                    message.CancelledAt == null &&
                    message.NextAttemptAt <= now &&
                    (message.ProcessingStartedAt == null ||
                     message.ProcessingStartedAt < expiredLease))
                .ExecuteUpdateAsync(update => update
                    .SetProperty(message => message.ProcessingStartedAt, now)
                    .SetProperty(message => message.AttemptCount, message => message.AttemptCount + 1),
                    cancellationToken);

            if (claimed == 0)
            {
                return;
            }

            var outboxMessage = await context.NotificationOutboxMessages
                .Include(message => message.Notification)
                .Include(message => message.DeviceToken)
                .FirstOrDefaultAsync(message => message.Id == messageId, cancellationToken);

            if (outboxMessage is null)
            {
                return;
            }

            if (!_pushSender.IsConfigured)
            {
                ScheduleRetry(outboxMessage, "FcmNotConfigured");
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            if (outboxMessage.Topic is null &&
                (outboxMessage.DeviceToken is null ||
                 !outboxMessage.DeviceToken.IsActive ||
                 outboxMessage.DeviceToken.UserId != outboxMessage.Notification.UserId))
            {
                Cancel(outboxMessage, now, "The device token is no longer active for this recipient.");
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            var eventData = new Dictionary<string, string>
            {
                ["notificationId"] = outboxMessage.NotificationId.ToString(),
                ["eventKey"] = outboxMessage.Notification.EventKey ?? string.Empty
            };

            try
            {
                if (outboxMessage.Topic is not null)
                {
                    await _pushSender.SendToTopicAsync(
                        outboxMessage.Topic,
                        "Kirana Store",
                        outboxMessage.Notification.Message,
                        eventData,
                        cancellationToken);
                }
                else
                {
                    await _pushSender.SendAsync(
                        outboxMessage.DeviceToken!.Token,
                        "Kirana Store",
                        outboxMessage.Notification.Message,
                        eventData,
                        cancellationToken);
                }

                outboxMessage.DeliveredAt = DateTime.UtcNow;
                outboxMessage.ProcessingStartedAt = null;
                outboxMessage.LastError = null;
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (InvalidFcmTokenException)
            {
                if (outboxMessage.DeviceToken is not null)
                {
                    outboxMessage.DeviceToken.IsActive = false;
                }

                Cancel(outboxMessage, DateTime.UtcNow, "Firebase reports that the registration token is unregistered.");
                await context.SaveChangesAsync(cancellationToken);
                if (outboxMessage.DeviceToken is not null)
                {
                    _logger.LogInformation(
                        "Deactivated an unregistered FCM token for user {UserId}.",
                        outboxMessage.DeviceToken.UserId);
                }
            }
            catch (FirebaseMessagingException exception)
            {
                ScheduleRetry(outboxMessage, exception.GetType().Name);
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogWarning(
                    "FCM delivery failed for notification {NotificationId}; retry scheduled.",
                    outboxMessage.NotificationId);
            }
            catch (FcmNotConfiguredException)
            {
                ScheduleRetry(outboxMessage, "FcmNotConfigured");
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                ScheduleRetry(outboxMessage, exception.GetType().Name);
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogWarning(
                    "FCM delivery failed for notification {NotificationId}; retry scheduled.",
                    outboxMessage.NotificationId);
            }
        }

        private static void Cancel(
            Models.NotificationOutboxMessage message,
            DateTime now,
            string reason)
        {
            message.CancelledAt = now;
            message.ProcessingStartedAt = null;
            message.LastError = reason;
        }

        private static void ScheduleRetry(
            Models.NotificationOutboxMessage message,
            string errorCode)
        {
            var delaySeconds = Math.Min(
                3600,
                15 * Math.Pow(2, Math.Min(message.AttemptCount - 1, 8)));

            message.ProcessingStartedAt = null;
            message.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySeconds);
            message.LastError = errorCode;
        }
    }
}