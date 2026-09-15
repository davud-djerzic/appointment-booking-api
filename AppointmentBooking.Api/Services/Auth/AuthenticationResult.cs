namespace AppointmentBooking.Api.Services.Auth
{
    public sealed record AuthenticationResult(string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        string RefreshToken,
        DateTimeOffset RefreshTokenExpiresAt);
}
