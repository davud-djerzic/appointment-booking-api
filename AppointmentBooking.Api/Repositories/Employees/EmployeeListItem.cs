namespace AppointmentBooking.Api.Repositories.Employees
{
    public sealed class EmployeeListItem
    {
        public long Id { get; init; }

        public string FirstName { get; init; } = null!;

        public string LastName { get; init; } = null!;

        public string Email { get; init; } = null!;

        public string Phone { get; init; } = null!;

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset UpdatedAt { get; init; }
    }
}
