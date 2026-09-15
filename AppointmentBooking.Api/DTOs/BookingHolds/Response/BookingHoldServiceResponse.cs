namespace AppointmentBooking.Api.DTOs.BookingHolds.Response
{
    public sealed record BookingHoldServiceResponse(
        long Id,
        string Name,
        int DurationMinutes,
        decimal Price,
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt);
}
