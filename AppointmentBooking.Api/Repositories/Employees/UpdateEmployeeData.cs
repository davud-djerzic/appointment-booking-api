namespace AppointmentBooking.Api.Repositories.Employees
{
    public sealed record UpdateEmployeeData
    {
        public long Id { get; set; }
        public required string FirstName { get; init; }

        public required string LastName { get; init; }

        public required string Email { get; init; }

        public required string Phone { get; init; }
    }
}
