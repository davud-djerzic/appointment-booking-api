namespace AppointmentBooking.Api.Configuration
{
    public sealed class MailjetOptions
    {
        public const string SectionName = "Mailjet";

        public required string ApiKey { get; init; }

        public required string SecretKey { get; init; }

        public required string FromEmail { get; init; }

        public required string FromName { get; init; }
    }
}
