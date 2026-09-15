using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.UserAccounts
{
    public interface IUserAccountRepository
    {
        Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken);

        Task<UserAccount?> GetByIdAsync(long id, CancellationToken cancellationToken);
    }
}
