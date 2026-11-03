using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;
using ShopAPI.Models;

namespace ShopAPI.Services
{
    public sealed class NotificationEventService
    {
        public const string BroadcastTopic = "kirana_store_announcements";

        private readonly AppDbContext _context;

        public NotificationEventService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(
            int recipientUserId,
            string message,
            string eventKey,
            CancellationToken cancellationToken)
        {
            if (await _context.Notifications.AnyAsync(
                    notification => notification.EventKey == eventKey,
                    cancellationToken))
            {
                return;
            }

            var notification = new Notification
            {
                UserId = recipientUserId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                EventKey = eventKey
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            var activeDeviceTokenIds = await _context.DeviceTokens
                .Where(deviceToken =>
                    deviceToken.UserId == recipientUserId && deviceToken.IsActive)
                .Select(deviceToken => deviceToken.Id)
                .ToListAsync(cancellationToken);

            foreach (var deviceTokenId in activeDeviceTokenIds)
            {
                _context.NotificationOutboxMessages.Add(new NotificationOutboxMessage
                {
                    NotificationId = notification.Id,
                    DeviceTokenId = deviceTokenId,
                    NextAttemptAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task CreateBroadcastAsync(
            string message,
            string eventKey,
            string topic,
            CancellationToken cancellationToken)
        {
            if (await _context.Notifications.AnyAsync(
                    notification => notification.EventKey == eventKey,
                    cancellationToken))
            {
                return;
            }

            var transaction = _context.Database.CurrentTransaction is null
                ? await _context.Database.BeginTransactionAsync(cancellationToken)
                : null;

            try
            {
                var notification = new Notification
                {
                    UserId = null,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    EventKey = eventKey
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync(cancellationToken);

                _context.NotificationOutboxMessages.Add(new NotificationOutboxMessage
                {
                    NotificationId = notification.Id,
                    Topic = topic,
                    NextAttemptAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync(cancellationToken);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }
    }
}