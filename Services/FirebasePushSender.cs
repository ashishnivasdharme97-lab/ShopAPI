using FirebaseAdmin.Messaging;

namespace ShopAPI.Services
{
    public sealed class FirebasePushSender : IFcmPushSender
    {
        private readonly FirebaseMessaging? _messaging;

        public FirebasePushSender(FirebaseMessaging? messaging)
        {
            _messaging = messaging;
        }

        public bool IsConfigured => _messaging is not null;

        public async Task SendAsync(
            string token,
            string title,
            string body,
            IReadOnlyDictionary<string, string> data,
            CancellationToken cancellationToken)
        {
            if (_messaging is null)
            {
                throw new FcmNotConfiguredException();
            }

            var message = new Message
            {
                Token = token,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = title,
                    Body = body
                },
                Data = new Dictionary<string, string>(data)
            };

            try
            {
                await _messaging.SendAsync(message, cancellationToken);
            }
            catch (FirebaseMessagingException exception)
                when (exception.MessagingErrorCode is
                    MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
            {
                throw new InvalidFcmTokenException();
            }
        }

        public async Task SendToTopicAsync(
            string topic,
            string title,
            string body,
            IReadOnlyDictionary<string, string> data,
            CancellationToken cancellationToken)
        {
            if (_messaging is null)
            {
                throw new FcmNotConfiguredException();
            }

            var message = new Message
            {
                Topic = topic,
                Notification = new FirebaseAdmin.Messaging.Notification
                {
                    Title = title,
                    Body = body
                },
                Data = new Dictionary<string, string>(data)
            };

            await _messaging.SendAsync(message, cancellationToken);
        }
    }
}