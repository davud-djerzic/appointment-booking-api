using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.EmployeeWorkingHoursRepository
{
    public interface IEmployeeWorkingHoursRepository
    {
        Task<EmployeeWorkingHours> CreateAsync(EmployeeWorkingHours employeeWorkingHours, CancellationToken cancellationToken);
        Task<EmployeeWorkingHours?> GetByIdAsync(long employeeId, long workingHoursId, CancellationToken cancellationToken);

        Task<IReadOnlyCollection<EmployeeWorkingHours>> GetByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken);

        Task<EmployeeWorkingHours> UpdateAsync(long employeeId, long workingHoursId, EmployeeWorkingHours workingHours, CancellationToken cancellationToken);

        Task DeleteAsync(long employeeId, long workingHoursId, CancellationToken cancellationToken);

    }
}
