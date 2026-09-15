namespace AppointmentBooking.Api.Models
{
    public sealed class UserRefreshToken
    {
        public long Id { get; init; }

        public long UserAccountId { get; init; }

        public Guid FamilyId { get; init; }

        public string TokenHash { get; init; } = null!;

        public DateTimeOffset ExpiresAt { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? RevokedAt { get; init; }

        public long? ReplacedByTokenId { get; init; }
    }
}
