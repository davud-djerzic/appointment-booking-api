namespace AppointmentBooking.Api.DTOs.EmployeeServices.Responses
{
    public sealed record EmployeeServiceAssignmentResponse(long EmployeeId, long ServiceId, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
   
}
