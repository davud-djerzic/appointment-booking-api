namespace AppointmentBooking.Api.Repositories.Bookings
{
    public sealed class BookingTimeRange
    {
        public DateTimeOffset StartsAt { get; init; }
        public DateTimeOffset EndsAt { get; init; }
    }
}
