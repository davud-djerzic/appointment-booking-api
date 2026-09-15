namespace AppointmentBooking.Api.Models
{
    public sealed class BookingHold
    {
        public Guid HoldToken { get; init; }

        public long CustomerId { get; init; }

        public long EmployeeId { get; init; }

        public DateTimeOffset StartsAt { get; init; }

        public DateTimeOffset EndsAt { get; init; }

        public IReadOnlyList<BookingHoldService> Services { get; init; } = [];

        public string? Notes { get; init; }

        public DateTimeOffset ExpiresAt { get; init; }
    }
}
