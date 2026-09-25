using AppointmentBooking.Api.DTOs.Salons.Request;
using AppointmentBooking.Api.DTOs.Salons.Response;

namespace AppointmentBooking.Api.Services.Salons
{
    public interface ISalonService
    {
        Task<SalonResponse> CreateAsync(CreateSalonRequest request, CancellationToken cancellationToken);

        Task<SalonResponse> GetByIdAsync(long salonId, CancellationToken cancellationToken);
    }
}
