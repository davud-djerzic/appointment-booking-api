using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.DTOs.Appointments.Response
{
    public sealed record AppointmentResponse(
        long Id,
        long BookingId,
        long ServiceId,
        string ServiceName,
        int DurationMinutes,
        decimal Price,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        AppointmentStatus Status);
    
}
