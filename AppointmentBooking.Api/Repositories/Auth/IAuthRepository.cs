using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Auth
{
    public interface IAuthRepository
    {
        Task<UserAccount> CreateCustomerAccountAsync(UserAccount userAccount, Customer customer, Guid familyId, string refreshTokenHash, DateTimeOffset refreshTokenExpiresAt, CancellationToken cancellationToken);
        Task CreateRefreshTokenAsync(long userAccountId, Guid familyId, string refreshTokenHash, DateTimeOffset expiresAt, CancellationToken cancellationToken);

        Task<UserRefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken);

        Task<RefreshTokenRotationResult?> RotateRefreshTokenAsync(long currentRefreshTokenId, string newRefreshTokenHash, DateTimeOffset newRefreshTokenExpiresAt, CancellationToken cancellationToken);

        Task RevokeRefreshTokenAsync(long refreshTokenid, CancellationToken cancellationToken); 

        Task RevokeRefreshTokenFamilyAsync(Guid familyId, CancellationToken cancellationToken);

        Task RevokeAllRefreshTokensAsync(long userAccountId, CancellationToken cancellationToken);
    }
}
