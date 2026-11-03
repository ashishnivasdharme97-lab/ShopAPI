namespace ShopAPI.Services
{
    public interface IFcmPushSender
    {
        bool IsConfigured { get; }

        Task SendAsync(
            string token,
            string title,
            string body,
            IReadOnlyDictionary<string, string> data,
            CancellationToken cancellationToken);

        Task SendToTopicAsync(
            string topic,
            string title,
            string body,
            IReadOnlyDictionary<string, string> data,
            CancellationToken cancellationToken);
    }

    public sealed class InvalidFcmTokenException : Exception
    {
    }

    public sealed class FcmNotConfiguredException : Exception
    {
    }
}