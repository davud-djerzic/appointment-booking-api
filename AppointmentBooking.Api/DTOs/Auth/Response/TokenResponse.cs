namespace AppointmentBooking.Api.DTOs.Auth.Response
{
    public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt);
}
