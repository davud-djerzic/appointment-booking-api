using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Employees
{
    public interface IEmployeeRepository
    {
        Task<Employee> CreateAsync(Employee employee, CancellationToken cancellationToken);
        Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken);

        Task DeactivateAsync(long id, CancellationToken cancellationToken);

        Task<PagedResult<Employee>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken);

        Task<Employee?> UpdateAsync(UpdateEmployeeData employee, CancellationToken cancellationToken);

        Task<Employee?> ActivateAsync(long id, CancellationToken cancellationToken);
    }
}
