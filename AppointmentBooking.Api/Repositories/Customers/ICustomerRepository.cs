using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Customers
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetActiveByUserAccountIdAsync(long userAccountId, CancellationToken cancellationToken);
    }
}
