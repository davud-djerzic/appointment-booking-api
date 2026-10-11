using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Request;
using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Response;

namespace AppointmentBooking.Api.Services.EmployeeWorkingHoursService;

public interface IEmployeeRecurringBreakService
{
    Task<IReadOnlyCollection<EmployeeWorkingHoursResponse>> GetForCurrentEmployeeAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<EmployeeWorkingHoursResponse>> ApplyForCurrentEmployeeAsync(
        ApplyRecurringBreakRequest request,
        CancellationToken cancellationToken);
}
