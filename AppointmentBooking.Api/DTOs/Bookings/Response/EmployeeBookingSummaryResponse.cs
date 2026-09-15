using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.DTOs.Bookings.Response
{
    public sealed record EmployeeBookingSummaryResponse(
        long Id,
        CustomerSummaryResponse Customer,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        int TotalDurationMinutes,
        decimal TotalPrice,
        BookingStatus Status,
        string? Notes,
        DateTimeOffset CreatedAt);
}
