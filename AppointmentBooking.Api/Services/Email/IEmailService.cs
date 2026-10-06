namespace AppointmentBooking.Api.Services.Email
{
    public interface IEmailService
    {
        Task SendAsync(string toEmail, string? toName, string subject, string textPart, string htmlPart, CancellationToken cancellationToken);
    }
}
