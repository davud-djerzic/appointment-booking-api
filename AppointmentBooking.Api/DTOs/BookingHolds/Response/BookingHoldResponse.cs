namespace AppointmentBooking.Api.DTOs.BookingHolds.Response
{
    public sealed record BookingHoldResponse(
        Guid HoldToken,
        EmployeeSummaryResponse Employee,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt,
        int TotalDurationMinutes,
        decimal TotalPrice,
        IReadOnlyList<BookingHoldServiceResponse> Services,
        DateTimeOffset ExpiresAt);
}
