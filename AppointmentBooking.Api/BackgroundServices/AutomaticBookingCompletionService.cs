using AppointmentBooking.Api.Configuration;
using AppointmentBooking.Api.Repositories.Bookings;
using AppointmentBooking.Api.Services.Appointments;
using Microsoft.Extensions.Options;

namespace AppointmentBooking.Api.BackgroundServices
{
    public sealed class AutomaticBookingCompletionService(IServiceScopeFactory scopeFactory, ILogger<AutomaticBookingCompletionService> logger, IOptions<BookingOptions> options, TimeProvider timeProvider) : BackgroundService
    {
        private readonly BookingOptions bookingOptions = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!bookingOptions.EnableAutomaticCompletion)
            {
                logger.LogInformation("Automatic booking completion service is disabled.");

                return;
            }

            logger.LogInformation("Automatic booking completion service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DateTimeOffset now = timeProvider.GetLocalNow();

                    DateTimeOffset nextRun = GetNextRunTime(now);

                    TimeSpan delay = nextRun - now;

                    logger.LogInformation(
                        "Next automatic booking completion scheduled for {NextRun}.",
                        nextRun);

                    await Task.Delay(
                        delay,
                        stoppingToken);

                    await CompleteBookingsAsync(
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "An error occurred during automatic booking completion.");

                    await Task.Delay(
                        TimeSpan.FromMinutes(1),
                        stoppingToken);
                }
            }

            logger.LogInformation(
                "Automatic booking completion service stopped.");
        }

        private async Task CompleteBookingsAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();

            IBookingRepository bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            DateTimeOffset completedAt = timeProvider.GetLocalNow();

            int completedCount = await bookingRepository.CompleteExpiredAutomaticallyAsync(completedAt, cancellationToken);

            logger.LogInformation("Automatic booking completion finished. {CompletedCount} bookings were completed.", completedCount);
        }

        private DateTimeOffset GetNextRunTime(DateTimeOffset now)
        {
            DateTimeOffset nextRun =
                new DateTimeOffset(
                    now.Year,
                    now.Month,
                    now.Day,
                    bookingOptions.AutomaticCompletionTime.Hour,
                    bookingOptions.AutomaticCompletionTime.Minute,
                    0,
                    now.Offset);

            if (nextRun <= now)
            {
                nextRun = nextRun.AddDays(1);
            }

            return nextRun;
        }
    }
}
