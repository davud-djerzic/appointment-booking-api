using AppointmentBooking.Api.Configuration;
using AppointmentBooking.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AppointmentBooking.Api.Services.Auth
{
    public class AccessTokenService(IOptions<JwtOptions> options) : IAccessTokenService
    {
        private readonly JwtOptions jwtOptions = options.Value;

        public AccessTokenResult CreateAccessToken(UserAccount userAccount)
        {
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.AccessTokenMinutes);

            List<Claim> claims =
            [
                new(JwtRegisteredClaimNames.Sub, userAccount.Id.ToString()),

                new(JwtRegisteredClaimNames.Email, userAccount.Email),

                new(ClaimTypes.Role, userAccount.Role.ToString()),

                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ];

            SymmetricSecurityKey securityKey =new(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

            SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token =
                new(
                    issuer: jwtOptions.Issuer,
                    audience: jwtOptions.Audience,
                    claims: claims,
                    expires: expiresAt.UtcDateTime,
                    signingCredentials: credentials);

            string serializedToken = new JwtSecurityTokenHandler().WriteToken(token);

            return new AccessTokenResult(serializedToken, expiresAt);
        }
    }
}
