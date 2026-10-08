using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.Models
{
    public sealed class EmployeeUnavailability
    {
        public long Id { get; init; }
        public long EmployeeId { get; init; }
        public EmployeeUnavailabilityType Type { get; init; }
        public DateTimeOffset StartsAt { get; init; }
        public DateTimeOffset EndsAt { get; init; }
        public bool IsAllDay { get; init; }
        public string? Note { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
