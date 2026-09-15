namespace AppointmentBooking.Api.Services.Auth
{
    public interface IRefreshTokenService
    {
        string GenerateToken();

        string HashToken(string token);
    }
}
