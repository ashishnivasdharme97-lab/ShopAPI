using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;

namespace ShopAPI.Services
{
    public sealed class FestivalReminderWorker : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);
        private static readonly TimeZoneInfo MaharashtraTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FestivalReminderWorker> _logger;

        public FestivalReminderWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<FestivalReminderWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await QueueDueRemindersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Festival reminder scheduling failed; the next poll will retry.");
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
        }

        private async Task QueueDueRemindersAsync(CancellationToken cancellationToken)
        {
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MaharashtraTimeZone);
            var reminderDate = DateOnly.FromDateTime(localNow.Date).AddDays(7);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notificationEvents =
                scope.ServiceProvider.GetRequiredService<NotificationEventService>();

            var festivals = await context.FestivalSchedules
                .Where(festival => festival.IsActive && festival.FestivalDate == reminderDate)
                .ToListAsync(cancellationToken);

            if (festivals.Count == 0)
            {
                return;
            }

            foreach (var festival in festivals)
            {
                await notificationEvents.CreateBroadcastAsync(
                    $"Reminder: {festival.Name} is in 7 days ({festival.FestivalDate:dd MMM yyyy}).",
                    $"festival:{festival.Id}:7-days:broadcast",
                    NotificationEventService.BroadcastTopic,
                    cancellationToken);
            }

            _logger.LogInformation(
                "Queued seven-day reminders for {FestivalCount} Maharashtra festivals on the global announcement topic.",
                festivals.Count);
        }
    }
}