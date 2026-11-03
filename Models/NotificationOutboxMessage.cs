namespace ShopAPI.Models
{
    public class NotificationOutboxMessage
    {
        public int Id { get; set; }

        public int NotificationId { get; set; }

        public Notification Notification { get; set; } = null!;

        public int? DeviceTokenId { get; set; }

        public DeviceToken? DeviceToken { get; set; }

        public string? Topic { get; set; }

        public int AttemptCount { get; set; }

        public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessingStartedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        public string? LastError { get; set; }
    }
}