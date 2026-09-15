using AppointmentBooking.Api.DTOs.Appointments.Response;
using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.DTOs.Bookings.Response
{
    public sealed record BookingResponse(
        long Id,
        long CustomerId,
        long EmployeeId,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        BookingStatus Status,
        string? Notes,
        DateTimeOffset CreatedAt,
        IReadOnlyList<AppointmentResponse> Appointments);
}
