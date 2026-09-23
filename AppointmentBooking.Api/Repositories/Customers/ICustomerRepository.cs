using AppointmentBooking.Api.DTOs.Profile.Request;
using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Customers
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetActiveByUserAccountIdAsync(long userAccountId, CancellationToken cancellationToken);

        Task UpdateProfileAsync(long userAccountId, UpdateMyProfileRequest request, CancellationToken cancellationToken);
    }
}
