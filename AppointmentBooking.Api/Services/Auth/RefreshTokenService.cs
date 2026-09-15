using System.Security.Cryptography;
using System.Text;

namespace AppointmentBooking.Api.Services.Auth
{
    public sealed class RefreshTokenService : IRefreshTokenService
    {
        public string GenerateToken()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(64);

            return Convert.ToBase64String(bytes);
        }

        public string HashToken(string token)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(hash);
        }
    }
}
