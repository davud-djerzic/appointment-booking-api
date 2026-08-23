using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Response
{
    public sealed record EmployeeWorkingHoursResponse(long Id, long EmployeeId, WeekDay DayOfWeek, TimeOnly StartsAt, TimeOnly EndsAt);

}
