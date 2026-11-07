namespace AppointmentBooking.Api.Configuration
{
    public sealed class BookingOptions
    {
        public const string SectionName = "Bookings";

        public int HoldDurationInMinutes { get; init; }

        public int AvailabilitySlotIntervalMinutes { get; init; }

        // Upcoming (scheduled, not yet started) bookings one customer may have at the same time.
        // Staff bookings are not limited. Railway variable: Bookings__MaxActiveBookingsPerCustomer.
        public int MaxActiveBookingsPerCustomer { get; init; } = 5;
    }
}
