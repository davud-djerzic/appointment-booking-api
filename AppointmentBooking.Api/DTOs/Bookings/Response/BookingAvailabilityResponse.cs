namespace AppointmentBooking.Api.DTOs.Bookings.Response
{
    public sealed record BookingAvailabilityResponse(
    long EmployeeId,
    DateOnly Date,
    int DurationMinutes,
    IReadOnlyList<AvailableTimeSlotResponse> Slots);
}
