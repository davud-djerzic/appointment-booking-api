namespace AppointmentBooking.Api.Configuration
{
    public sealed class BookingOptions
    {
        public const string SectionName = "Bookings";

        public int HoldDurationInMinutes { get; init; }

        public int CleanupIntervalSeconds { get; init; }

        public TimeOnly AutomaticCompletionTime { get; init; }

        public bool EnableAutomaticCompletion { get; init; }

        public int AvailabilitySlotIntervalMinutes { get; init; }
    }
}
