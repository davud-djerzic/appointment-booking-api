namespace AppointmentBooking.Api.Services.Email
{
    public sealed record EmailMessage(
        string ToEmail,
        string? ToName,
        string Subject,
        string TextPart,
        string HtmlPart);
}
