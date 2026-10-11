using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Request;
using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Response;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.Employees;
using AppointmentBooking.Api.Repositories.EmployeeWorkingHoursRepository;
using AppointmentBooking.Api.Services.CurrentUser;

namespace AppointmentBooking.Api.Services.EmployeeWorkingHoursService;

public sealed class EmployeeRecurringBreakService(
    IEmployeeWorkingHoursRepository workingHoursRepository,
    IEmployeeRepository employeeRepository,
    ICurrentUserService currentUser) : IEmployeeRecurringBreakService
{
    public async Task<IReadOnlyCollection<EmployeeWorkingHoursResponse>> GetForCurrentEmployeeAsync(
        CancellationToken cancellationToken)
    {
        Employee employee = await GetCurrentEmployeeAsync(cancellationToken);
        IReadOnlyCollection<EmployeeWorkingHours> hours =
            await workingHoursRepository.GetByEmployeeIdAsync(employee.Id, cancellationToken);

        return Map(hours);
    }

    public async Task<IReadOnlyCollection<EmployeeWorkingHoursResponse>> ApplyForCurrentEmployeeAsync(
        ApplyRecurringBreakRequest request,
        CancellationToken cancellationToken)
    {
        Employee employee = await GetCurrentEmployeeAsync(cancellationToken);

        IReadOnlyCollection<EmployeeWorkingHours> hours =
            await workingHoursRepository.ApplyRecurringBreakAsync(
                employee.Id,
                request.DayOfWeek,
                request.StartsAt,
                request.EndsAt,
                cancellationToken);

        return Map(hours);
    }

    private async Task<Employee> GetCurrentEmployeeAsync(CancellationToken cancellationToken)
    {
        Employee? employee = await employeeRepository.GetByUserAccountIdAsync(
            currentUser.UserAccountId,
            cancellationToken);

        if (employee is null)
        {
            throw new NotFoundException("The current user does not have an employee profile.");
        }

        if (!employee.IsActive)
        {
            throw new ConflictException("The current employee profile is inactive.");
        }

        return employee;
    }

    private static IReadOnlyCollection<EmployeeWorkingHoursResponse> Map(
        IEnumerable<EmployeeWorkingHours> hours)
    {
        return hours.Select(hour => new EmployeeWorkingHoursResponse(
            hour.Id,
            hour.EmployeeId,
            hour.DayOfWeek,
            hour.StartsAt,
            hour.EndsAt)).ToArray();
    }
}
