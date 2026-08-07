using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.DTOs.Appointments.Response
{
    public sealed record AppointmentResponse(
        long Id,
        long EmployeeId,
        long ServiceId,
        string CustomerFirstName,
        string CustomerLastName,
        string? CustomerEmail,
        string? CustomerPhone,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        AppointmentStatus Status,
        string? Notes,
        DateTimeOffset CreatedAt,
        DateTimeOffset? UpdatedAt
   );
}
