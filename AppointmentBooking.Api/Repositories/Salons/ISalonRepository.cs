using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Salons
{
    public interface ISalonRepository
    {
        Task<SalonWithWorkingHours> CreateAsync(Salon salon, IReadOnlyCollection<SalonWorkingHours> workingHours, CancellationToken cancellationToken);
        Task<SalonWithWorkingHours?> GetByIdAsync(long salonId, CancellationToken cancellationToken);

    }
}
