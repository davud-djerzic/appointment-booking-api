namespace AppointmentBooking.Api.DTOs.Bookings.Response
{
    public sealed record BookableEmployeeResponse(
     long Id,
     string FirstName,
     string LastName);
}
