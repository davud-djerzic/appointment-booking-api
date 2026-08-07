namespace AppointmentBooking.Api.Configuration
{
    public sealed class AppointmentOptions
    {
        public const string SectionName = "Appointments";

        public int HoldDurationInMinutes { get; init; }

        public int CleanupIntervalSeconds { get; init; }

        public TimeOnly AutomaticCompletionTime { get; init; }

        public bool EnableAutomaticCompletion { get; init; }

    }
}
