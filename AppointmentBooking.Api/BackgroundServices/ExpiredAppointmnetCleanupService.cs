using AppointmentBooking.Api.Configuration;
using AppointmentBooking.Api.Services.Appointments;
using Microsoft.Extensions.Options;

namespace AppointmentBooking.Api.BackgroundServices
{
    public sealed class ExpiredAppointmnetCleanupService(IServiceScopeFactory scopeFactory, ILogger<ExpiredAppointmnetCleanupService> logger, IOptions<BookingOptions> options) : BackgroundService
    {
        private readonly BookingOptions appointmentOptions = options.Value;
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            /*logger.LogInformation("Expired appointment hold cleanup service started.");

            while(!stoppingToken.IsCancellationRequested)
            {
                using IServiceScope scope = scopeFactory.CreateScope();

                IAppointmentService appointmentService = scope.ServiceProvider.GetRequiredService<IAppointmentService>();

                await appointmentService.DeleteExpiredHoldsAsync(stoppingToken);
                await Task.Delay(appointmentOptions.CleanupIntervalSeconds, stoppingToken);
            }*/
        }
    }
}
