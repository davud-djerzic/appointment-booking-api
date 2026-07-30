using AppointmentBooking.Api.DTOs.BookedServices.Request;
using AppointmentBooking.Api.DTOs.BookedServices.Response;
using AppointmentBooking.Api.DTOs.Common;

namespace AppointmentBooking.Api.Services.BookedServices
{
    public interface IBookableServiceService
    {
        Task<ServiceResponse> CreateAsync(CreateServiceRequest request, CancellationToken cancellationToken);

        Task<ServiceResponse?> GetByIdAsync(long id, CancellationToken cancellationToken);

        Task<bool> DeactivateAsync(long id, CancellationToken cancellationToken);

        Task<PagedResponse<ServiceResponse>> GetAllAsync(GetServicesQuery query, CancellationToken cancellationToken);

        Task<ServiceResponse?> UpdateAsync(long id, UpdateServiceRequest request, CancellationToken cancellationToken);

        Task<ServiceResponse?> ActivateAsync(long id, CancellationToken cancellationToken);
    }
}
