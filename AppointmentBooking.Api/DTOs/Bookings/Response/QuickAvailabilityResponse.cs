namespace AppointmentBooking.Api.DTOs.Bookings.Response
{
    public sealed record QuickAvailabilityResponse(
        long EmployeeId,
        long ServiceId,
        string ServiceName,
        int DurationMinutes,
        decimal Price,
        DateOnly? Date,
        IReadOnlyList<AvailableTimeSlotResponse> AvailableSlots);
}
