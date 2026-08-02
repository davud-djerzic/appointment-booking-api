using AppointmentBooking.Api.DTOs.EmployeeServices.Responses;

namespace AppointmentBooking.Api.Services.EmployeeServices
{
    public interface IEmployeeServiceAssignmentService
    {
        Task<AssignEmployeeServiceResult> AssignAsync(long employeeId, long serviceId, CancellationToken cancellationToken);

        Task<DeactivateEmployeeServiceResult> DeactivateAsync(long employeeId, long serviceId, CancellationToken cancellationToken);

        Task<IEnumerable<EmployeeServiceResponse>> GetEmployeeServicesAsync(long employeeId, CancellationToken cancellationToken);

        Task<IEnumerable<ServiceEmployeeResponse>> GetServiceEmployeesAsync(long serviceId, CancellationToken cancellationToken);

        Task<EmployeeServiceAssignmentResponse> GetServiceEmployeesAssignmentsAsync(long employeeId, long serviceId, CancellationToken cancellationToken);

    }
}
