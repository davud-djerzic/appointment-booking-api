using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.DTOs.Employees.Request;
using AppointmentBooking.Api.DTOs.Employees.Response;

namespace AppointmentBooking.Api.Services.Employees
{
    public interface IEmployeeService
    {
        Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken);

        Task<EmployeeResponse> GetByIdAsync(long id, CancellationToken cancellationToken);

        Task<bool> DeactivateAsync(long id, CancellationToken cancellationToken);

        Task<PagedResponse<EmployeeResponse>> GetAllAsync(GetEmployeesQuery query, CancellationToken cancellationToken);

        Task<EmployeeResponse?> UpdateAsync(long id, UpdateEmployeeRequest request, CancellationToken cancellationToken);

        Task<EmployeeResponse?> ActivateAsync(long id, CancellationToken cancellationToken);
    }
}
