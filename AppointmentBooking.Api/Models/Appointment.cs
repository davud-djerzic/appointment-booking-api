namespace AppointmentBooking.Api.Models
{
    public class Appointment
    {
        public long Id { get; init; }
        public long EmployeeId { get; init; }
        public long ServiceId { get; init; }
        public string? CustomerFirstName { get; init; }

        public string? CustomerLastName { get; init; }

        public string? CustomerEmail { get; init; }

        public string? CustomerPhone { get; init; }

        public DateTimeOffset StartsAt { get; init; }

        public DateTimeOffset EndsAt { get; init; }

        public required string Status { get; init; }

        public Guid? HoldToken { get; init; }

        public DateTimeOffset? HoldExpiresAt { get; init; }

        public string? Notes { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
