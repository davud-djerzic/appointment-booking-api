namespace AppointmentBooking.Api.Models
{
    public sealed class PasswordResetCode
    {
        public long Id { get; set; }

        public long UserAccountId { get; set; }

        public string CodeHash { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; set; }

        public short Attempts { get; set; }

        public DateTimeOffset? VerifiedAt { get; set; }

        public DateTimeOffset? UsedAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
