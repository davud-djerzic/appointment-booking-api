using AppointmentBooking.Api.DTOs.EmployeeServices.Responses;

namespace AppointmentBooking.Api.Services.EmployeeServices
{
    public sealed record AssignEmployeeServiceResult(AssignEmployeeServiceStatus Status, EmployeeServiceAssignmentResponse? Assignment = null, string? Message = null, string? Title = null);
}
