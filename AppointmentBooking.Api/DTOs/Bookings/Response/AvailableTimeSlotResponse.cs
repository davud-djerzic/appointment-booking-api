namespace AppointmentBooking.Api.DTOs.Bookings.Response
{
    public sealed record AvailableTimeSlotResponse(
        DateTimeOffset StartsAt,
        DateTimeOffset EndsAt);
}
