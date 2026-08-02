namespace AppointmentBooking.Api.DTOs.EmployeeServices.Responses
{
    public sealed record EmployeeServiceResponse(long Id, string Name, string? Description, decimal Price, int DurationMinutes);

}
