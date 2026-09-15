using AppointmentBooking.Api.Configuration;
using AppointmentBooking.Api.DTOs.Auth.Request;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Models.Enums;
using AppointmentBooking.Api.Repositories.Auth;
using AppointmentBooking.Api.Repositories.UserAccounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AppointmentBooking.Api.Services.Auth
{
    public sealed class AuthService(IUserAccountRepository userAccountRepository, IAuthRepository authRepository, 
        IPasswordHasher<UserAccount> passwordHasher, IAccessTokenService accessTokenService, 
        IRefreshTokenService refreshTokenService, IOptions<JwtOptions> jwtOptions) : IAuthService
    {
        private readonly JwtOptions options = jwtOptions.Value;

        public async Task<AuthenticationResult> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken cancellationToken)
        {
            string email = request.Email.Trim();

            UserAccount? existingUserAccount = await userAccountRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUserAccount is not null) throw new ConflictException("An account with this email already exists.");

            UserAccount userAccount = new()
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PasswordHash = string.Empty,
                Role = UserRole.Customer,
                IsActive = true
            };

            userAccount.PasswordHash = passwordHasher.HashPassword(userAccount, request.Password);

            Customer customer = new()
            {
                Phone = request.Phone.Trim()
            };

            Guid familyId = Guid.NewGuid();

            string refreshToken = refreshTokenService.GenerateToken();

            string refreshTokenHash = refreshTokenService.HashToken(refreshToken);

            DateTimeOffset refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(options.RefreshTokenDays);

            UserAccount createdUserAccount = await authRepository.CreateCustomerAccountAsync(userAccount, customer, familyId, refreshTokenHash, refreshTokenExpiresAt, cancellationToken);

            AccessTokenResult accessToken = accessTokenService.CreateAccessToken(createdUserAccount);

            return new AuthenticationResult(accessToken.Token, accessToken.ExpiresAt, refreshToken, refreshTokenExpiresAt);
        }

        public async Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        {
            string email = request.Email.Trim();

            UserAccount? userAccount = await userAccountRepository.GetByEmailAsync(email, cancellationToken);
            if (userAccount is null) throw new UnauthorizedException("Invalid email or password.");
            if (!userAccount.IsActive) throw new UnauthorizedException("Invalid email or password.");

            PasswordVerificationResult passwordResult = passwordHasher.VerifyHashedPassword(userAccount, userAccount.PasswordHash, request.Password);
            if (passwordResult == PasswordVerificationResult.Failed) throw new UnauthorizedException("Invalid email or password.");

            Guid familyId = Guid.NewGuid();

            string refreshToken = refreshTokenService.GenerateToken();

            string refreshTokenHash = refreshTokenService.HashToken(refreshToken);
            DateTimeOffset refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(options.RefreshTokenDays);

            await authRepository.CreateRefreshTokenAsync(userAccount.Id, familyId, refreshTokenHash,refreshTokenExpiresAt, cancellationToken);

            AccessTokenResult accessToken = accessTokenService.CreateAccessToken(userAccount);

            return new AuthenticationResult(accessToken.Token, accessToken.ExpiresAt, refreshToken, refreshTokenExpiresAt);
        }

        public async Task<AuthenticationResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            string refreshTokenHash = refreshTokenService.HashToken(refreshToken);

            UserRefreshToken? storedRefreshToken = await authRepository.GetRefreshTokenByHashAsync(refreshTokenHash, cancellationToken);
            if (storedRefreshToken is null) throw new UnauthorizedException("Invalid refresh token.");

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (storedRefreshToken.RevokedAt is not null) {
                if (storedRefreshToken.ReplacedByTokenId is not null)
                {
                    await authRepository.RevokeRefreshTokenFamilyAsync(storedRefreshToken.FamilyId, cancellationToken);
                }

                throw new UnauthorizedException("Invalid refresh token.");
            }
           
            if (storedRefreshToken.ExpiresAt <= now) throw new UnauthorizedException("Invalid refresh token.");

            UserAccount? userAccount = await userAccountRepository.GetByIdAsync(storedRefreshToken.UserAccountId, cancellationToken);
            if (userAccount is null || !userAccount.IsActive) throw new UnauthorizedException("Invalid refresh token.");

            string newRefreshToken = refreshTokenService.GenerateToken();

            string newRefreshTokenHash = refreshTokenService.HashToken(newRefreshToken);

            DateTimeOffset newRefreshTokenExpiresAt = now.AddDays(options.RefreshTokenDays);

            RefreshTokenRotationResult? rotationResult = await authRepository.RotateRefreshTokenAsync(storedRefreshToken.Id, newRefreshTokenHash, newRefreshTokenExpiresAt, cancellationToken);
            if (rotationResult is null) throw new UnauthorizedException("Invalid refresh token.");

            AccessTokenResult accessToken = accessTokenService.CreateAccessToken(userAccount);

            return new AuthenticationResult(
                accessToken.Token,
                accessToken.ExpiresAt,
                newRefreshToken,
                newRefreshTokenExpiresAt);
        }

        public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        {
            string refreshTokenHash = refreshTokenService.HashToken(refreshToken);

            UserRefreshToken? storedRefreshToken = await authRepository.GetRefreshTokenByHashAsync(refreshTokenHash, cancellationToken);
            if (storedRefreshToken is null) return;
            if (storedRefreshToken.RevokedAt is not null) return;

            await authRepository.RevokeRefreshTokenAsync(storedRefreshToken.Id, cancellationToken);
        }
    }
}
