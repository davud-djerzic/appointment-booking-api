namespace AppointmentBooking.Api.Services.Email
{
    /// <summary>Sends queued emails one by one through IEmailService (Mailjet).</summary>
    public sealed class EmailBackgroundService(
        EmailQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailBackgroundService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (EmailMessage message in queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using IServiceScope scope = scopeFactory.CreateScope();
                    IEmailService emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    await emailService.SendAsync(
                        message.ToEmail,
                        message.ToName,
                        message.Subject,
                        message.TextPart,
                        message.HtmlPart,
                        stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // No address in the log (personal data).
                    logger.LogError(ex, "Sending email \"{Subject}\" failed.", message.Subject);
                }
            }
        }
    }
}
