using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.DTOs.EmployeeUnavailability.Response
{
    public sealed record EmployeeUnavailabilityResponse(
        long Id,
        long EmployeeId,
        EmployeeUnavailabilityType Type,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        bool IsAllDay,
        string? Note,
        DateTimeOffset CreatedAt);
}
