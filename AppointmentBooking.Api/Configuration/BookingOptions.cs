namespace AppointmentBooking.Api.Configuration
{
    public sealed class BookingOptions
    {
        public const string SectionName = "Bookings";

        public int HoldDurationInMinutes { get; init; }

        public int AvailabilitySlotIntervalMinutes { get; init; }
    }
}
