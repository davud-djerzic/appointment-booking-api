namespace AppointmentBooking.Api.Services.Email
{
    /// <summary>
    /// Emails are sent in the background, so a slow or failing Mailjet call never
    /// delays or fails the request that triggered the email.
    /// </summary>
    public interface IEmailQueue
    {
        void Enqueue(EmailMessage message);
    }
}
