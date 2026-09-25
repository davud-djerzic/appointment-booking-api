using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.DTOs.Salons.Response
{
    public sealed record SalonWorkingHoursResponse(
        WeekDay DayOfWeek,
        TimeOnly StartsAt,
        TimeOnly EndsAt);
}
