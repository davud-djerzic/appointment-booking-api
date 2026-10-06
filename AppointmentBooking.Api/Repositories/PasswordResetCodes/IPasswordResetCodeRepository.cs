using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.PasswordResetCodes
{
    public interface IPasswordResetCodeRepository
    {
        Task CreateAsync(PasswordResetCode passwordResetCode, CancellationToken cancellationToken);

        Task<PasswordResetCode?> GetLatestActiveAsync(long userAccountId, CancellationToken cancellationToken);

        Task<bool> IncrementAttemptsAsync(long id, CancellationToken cancellationToken);

        Task<bool> MarkVerifiedAsync(long id, CancellationToken cancellationToken);

        Task<bool> MarkUsedAsync(long id, CancellationToken cancellationToken);

        Task InvalidateActiveAsync(long userAccountId, CancellationToken cancellationToken);
    }
}
