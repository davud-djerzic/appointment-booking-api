using AppointmentBooking.Api.Configuration;
using AppointmentBooking.Api.Services.Appointments;
using Microsoft.Extensions.Options;

namespace AppointmentBooking.Api.BackgroundServices
{
    public sealed class AutomaticAppointmentCompletionService(IServiceScopeFactory scopeFactory, ILogger<AutomaticAppointmentCompletionService> logger, IOptions<AppointmentOptions> options, TimeProvider timeProvider) : BackgroundService
    {
        private readonly AppointmentOptions appointmentOptions = options.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!appointmentOptions.EnableAutomaticCompletion)
            {
                logger.LogInformation("Automatic appointment completion service is disabled.");
                return;
            }

            logger.LogInformation("Automatic appointment completion service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CompleteAppointmentsAsync(stoppingToken);

                    DateTimeOffset now = timeProvider.GetLocalNow();
                    DateTimeOffset nextRun = GetNextRunTime(now);

                    TimeSpan delay = nextRun - now;

                    logger.LogInformation(
                       "Next automatic completion scheduled for {NextRun}.",
                       nextRun);

                    await Task.Delay(delay, stoppingToken);
                } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while completing appointments.");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            logger.LogInformation("Automatic appointment completion service stopped.");
        }

        private async Task CompleteAppointmentsAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();

            IAppointmentService appointmentService =
                scope.ServiceProvider.GetRequiredService<IAppointmentService>();

            await appointmentService.CompleteExpiredAppointmentsAsync(cancellationToken);
        }

        private DateTimeOffset GetNextRunTime(DateTimeOffset now)
        {
            DateTimeOffset nextRun = new DateTimeOffset(
                    now.Year,
                    now.Month,
                    now.Day,
                    appointmentOptions.AutomaticCompletionTime.Hour,
                    appointmentOptions.AutomaticCompletionTime.Minute,
                    0,
                    now.Offset);

            if (nextRun <= now) nextRun = nextRun.AddDays(1);
            
            return nextRun;
        }
    }
}
