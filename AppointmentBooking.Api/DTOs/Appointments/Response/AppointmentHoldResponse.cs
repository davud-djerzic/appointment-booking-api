namespace AppointmentBooking.Api.DTOs.Appointments.Response
{
    public sealed record AppointmentHoldResponse(long id, Guid HoldToken, DateTimeOffset ExpiresAt);

}
