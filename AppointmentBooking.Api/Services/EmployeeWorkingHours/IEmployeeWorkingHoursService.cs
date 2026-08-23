using AppointmentBooking.Api.DTOs.BookedServices.Request;
using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Request;
using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Response;

namespace AppointmentBooking.Api.Services.EmployeeWorkingHoursService
{
    public interface IEmployeeWorkingHoursService
    {
        Task<EmployeeWorkingHoursResponse> CreateAsync(long employeeId, CreateEmployeeWorkingHoursRequest request, CancellationToken cancellationToken);
        Task<EmployeeWorkingHoursResponse> GetByIdAsync(long employeeId, long workingHoursId, CancellationToken cancellationToken);

        Task<IReadOnlyCollection<EmployeeWorkingHoursResponse>> GetByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken);

        Task<EmployeeWorkingHoursResponse> UpdateAsync(long employeeId, long workingHoursId, UpdateEmployeeWorkingHoursRequest request, CancellationToken cancellationToken);

        Task DeleteAsync(long employeeId, long workingHoursId, CancellationToken cancellationToken);

    }
}
