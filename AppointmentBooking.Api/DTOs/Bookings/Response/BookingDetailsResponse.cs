using AppointmentBooking.Api.DTOs.Appointments.Response;
using AppointmentBooking.Api.DTOs.BookingHolds.Response;
using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.DTOs.Bookings.Response
{
    public sealed record BookingDetailsResponse(
        long Id,
        EmployeeSummaryResponse Employee,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        int TotalDurationMinutes,
        decimal TotalPrice,
        BookingStatus Status,
        string? Notes,
        DateTimeOffset CreatedAt,
        DateTimeOffset? CompletedAt,
        CompletionSource? CompletionSource,
        IReadOnlyList<AppointmentResponse> Appointments);
}
