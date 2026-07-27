namespace AppointmentBooking.Api.DTOs.Employees.Response
{
    public sealed class EmployeeResponse
    {
        public long Id { get; set; }
        public required string FirstName { get; init; }

        public required string LastName { get; init; }

        public required string Email { get; init; }

        public required string Phone { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
