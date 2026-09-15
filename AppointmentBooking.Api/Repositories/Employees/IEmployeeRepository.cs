using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Employees
{
    public interface IEmployeeRepository
    {
        Task<Employee> CreateEmployeeAccountAsync(UserAccount userAccount, string phone, CancellationToken cancellationToken);
        Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken);

        Task DeactivateAsync(long id, CancellationToken cancellationToken);

        Task<PagedResult<EmployeeListItem>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken);

        Task<Employee> UpdateAsync(UpdateEmployeeData employee, CancellationToken cancellationToken);

        Task<Employee> ActivateAsync(long id, CancellationToken cancellationToken);

        Task<Employee?> GetByUserAccountIdAsync(long userAccountId, CancellationToken cancellationToken);
    }
}
