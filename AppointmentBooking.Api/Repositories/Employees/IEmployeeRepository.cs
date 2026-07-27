using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Employees
{
    public interface IEmployeeRepository
    {
        Task<Employee> CreateAsync(Employee employee, CancellationToken cancellationToken);
        Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken);

        Task<bool> DeactivateAsync(long id, CancellationToken cancellationToken);

        Task<PagedResult<Employee>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken);

        Task<Employee?> UpdateAsync(Employee employee, CancellationToken cancellationToken);

        Task<Employee?> ActivateAsync(long id, CancellationToken cancellationToken);
    }
}
