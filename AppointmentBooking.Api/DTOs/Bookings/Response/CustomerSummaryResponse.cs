namespace AppointmentBooking.Api.DTOs.Bookings.Response
{
    public sealed record CustomerSummaryResponse(
        long Id,
        string FirstName,
        string LastName,
        string Email,
        string Phone);
}
