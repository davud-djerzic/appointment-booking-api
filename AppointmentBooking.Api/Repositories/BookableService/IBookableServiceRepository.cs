using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.BookedServices;

namespace AppointmentBooking.Api.Repositories.Services
{
    public interface IBookableServiceRepository
    {
        Task<BookableService> CreateAsync(BookableService service, CancellationToken cancellationToken);

        Task<BookableService?> GetByIdAsync(long id, CancellationToken cancellationToken);

        Task<bool> DeactivateAsync(long id, CancellationToken cancellationToken);

        Task<PagedResult<BookableService>> GetAllAsync(ServiceSearchCriteria criteria, CancellationToken cancellationToken);

        Task<BookableService?> UpdateAsync(UpdateServiceData service, CancellationToken cancellationToken);

        Task<BookableService?> ActivateAsync(long id, CancellationToken cancellationToken);

        Task<IReadOnlyCollection<BookableService>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken);
    }
}
