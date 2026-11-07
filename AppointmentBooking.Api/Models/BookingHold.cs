namespace AppointmentBooking.Api.Models
{
    public sealed class BookingHold
    {
        public Guid HoldToken { get; init; }

        // Customer hold (mobile booking): the customer who holds the time.
        public long? CustomerId { get; init; }

        // Staff hold (walk-in booking added by staff): the staff user who holds the time,
        // and the name of the client the booking is for.
        public long? StaffUserAccountId { get; init; }

        public string? CustomerFirstName { get; init; }

        public string? CustomerLastName { get; init; }

        public long EmployeeId { get; init; }

        public DateTimeOffset StartsAt { get; init; }

        public DateTimeOffset EndsAt { get; init; }

        public IReadOnlyList<BookingHoldService> Services { get; init; } = [];

        public string? Notes { get; init; }

        public DateTimeOffset ExpiresAt { get; init; }
    }
}
