namespace AppointmentBooking.Api.DTOs.Auth.Response
{
    public sealed record AuthenticationTokenResponse(
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt);
}
