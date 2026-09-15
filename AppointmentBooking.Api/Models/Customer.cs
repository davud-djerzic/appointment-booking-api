namespace AppointmentBooking.Api.Models
{
    public sealed class Customer
    {
        public long Id { get; init; }

        public long UserAccountId { get; init; }

        public string Phone { get; init; } = null!;

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset UpdatedAt { get; init; }
    }
}
