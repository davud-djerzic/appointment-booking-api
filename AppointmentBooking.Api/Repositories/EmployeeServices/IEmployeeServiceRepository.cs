using AppointmentBooking.Api.DTOs.EmployeeServices.Responses;
using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.EmployeeServices
{
    public interface IEmployeeServiceRepository
    {
        Task<EmployeeServiceAssignemnt?> GetAsync(long employeeId, long serviceId, CancellationToken cancellationToken);
        Task<EmployeeServiceAssignemnt> AssignOrReactivateAsync(long employeeId, long serviceId, CancellationToken cancellationToken);
        Task<bool> DeactivateAsync(long employeeId, long serviceId, CancellationToken cancellationToken);

        Task<IEnumerable<EmployeeServiceResponse>> GetEmployeeServicesAsync(long employeeId, CancellationToken cancellationToken);

        Task<IEnumerable<ServiceEmployeeResponse>> GetServiceEmployeesAsync(long serviceId, CancellationToken cancellationToken);

        Task<EmployeeServiceAssignemnt> GetServiceEmployeesAssignmentAsync(long employeeId, long serviceId, CancellationToken cancellationToken);

        Task<IReadOnlyCollection<EmployeeServiceAssignemnt>> GetByEmployeeAndServiceIdsAsync(long employeeId, IReadOnlyCollection<long> serviceIds, CancellationToken cancellationToken);
    }
}
