namespace AppointmentBooking.Api.Models
{
    public sealed class Employee
    {
        public long Id { get; init; }

        public long UserAccountId { get; init; }

        public string Phone { get; init; } = null!;

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset UpdatedAt { get; init; }
    }
}
