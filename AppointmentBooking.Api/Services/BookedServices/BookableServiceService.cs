using AppointmentBooking.Api.DTOs.BookedServices.Request;
using AppointmentBooking.Api.DTOs.BookedServices.Response;
using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.BookedServices;
using AppointmentBooking.Api.Repositories.Employees;
using AppointmentBooking.Api.Repositories.Services;

namespace AppointmentBooking.Api.Services.BookedServices
{
    public sealed class BookableServiceService(IBookableServiceRepository serviceRepository) : IBookableServiceService
    {
        public async Task<ServiceResponse> CreateAsync(CreateServiceRequest request, CancellationToken cancellationToken)
        {
            BookableService service = new BookableService
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                DurationMinutes = request.DurationMinutes,
                Price = request.Price
            };

            BookableService createdService = await serviceRepository.CreateAsync(service, cancellationToken);

            return MapToResponse(createdService);
        }

        public async Task<ServiceResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            var service = await serviceRepository.GetByIdAsync(id, cancellationToken);
            return service is null ? null : MapToResponse(service);
        }

        public Task<bool> DeactivateAsync(long id, CancellationToken cancellationToken)
        {
            return serviceRepository.DeactivateAsync(id, cancellationToken);
        }

        public async Task<PagedResponse<ServiceResponse>> GetAllAsync(GetServicesQuery query, CancellationToken cancellationToken)
        {
            var critera = new ServiceSearchCriteria
            {
                Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
                IsActive = query.IsActive,
                MinPrice = query.MinPrice,
                MaxPrice = query.MaxPrice,
                MinDuration = query.MinDuration,
                MaxDuration = query.MaxDuration,
                Page = query.Page,
                PageSize = query.PageSize,
            };

            var result = await serviceRepository.GetAllAsync(critera, cancellationToken);

            return new PagedResponse<ServiceResponse>
            {
                Items = result.Items.Select(MapToResponse).ToArray(),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = result.TotalCount,
            };
        }

        public async Task<ServiceResponse?> UpdateAsync(long id, UpdateServiceRequest request, CancellationToken cancellationToken)
        {
            UpdateServiceData service = new UpdateServiceData
            {
                Id = id,
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Price = request.Price,
                DurationMinutes = request.DurationMinutes
            };

            BookableService? updatedService = await serviceRepository.UpdateAsync(service, cancellationToken);

            return updatedService is null ? null : MapToResponse(updatedService);
        }

        public async Task<ServiceResponse?> ActivateAsync(long id, CancellationToken cancellationToken)
        {
            BookableService? service = await serviceRepository.ActivateAsync(id, cancellationToken);

            return service is null ? null : MapToResponse(service);
        }


        private static ServiceResponse MapToResponse(BookableService service) 
        {
            return new ServiceResponse
            {
                Id = service.Id,
                Name = service.Name,
                Description = service.Description,
                DurationMinutes = service.DurationMinutes,
                Price = service.Price,
                IsActive = service.IsActive,
                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt
            };
        }

    }
}
