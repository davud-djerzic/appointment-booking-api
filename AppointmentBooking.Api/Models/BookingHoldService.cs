namespace AppointmentBooking.Api.Models
{
    public class BookingHoldService
    {
        public long ServiceId { get; init; }

        public string Name { get; init; } = null!;

        public int DurationMinutes { get; init; }

        public decimal Price { get; init; }

        public DateTimeOffset StartsAt { get; init; }

        public DateTimeOffset EndsAt { get; init; }
    }
}
