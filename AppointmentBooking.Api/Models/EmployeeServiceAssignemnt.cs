namespace AppointmentBooking.Api.Models
{
    public sealed class EmployeeServiceAssignemnt
    {
        public long EmployeeId { get; init; }
        public long ServiceId { get; init; }

        public bool IsActive { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
