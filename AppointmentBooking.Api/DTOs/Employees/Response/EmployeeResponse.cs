namespace AppointmentBooking.Api.DTOs.Employees.Response
{
    public sealed record EmployeeResponse(
        long Id,
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        bool IsActive,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

}
