namespace AppointmentBooking.Api.DTOs.BookingHolds.Response
{
    public sealed record EmployeeSummaryResponse(
        long Id,
        string FirstName,
        string LastName);
}
