namespace AppointmentBooking.Api.DTOs.Bookings.Response
{
    public sealed record EmployeeBookingServiceResponse(
        long Id,
        string Name,
        int DurationMinutes,
        decimal Price);
}
