using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Salons
{
    public sealed record SalonWithWorkingHours(
        Salon Salon,
        IReadOnlyCollection<SalonWorkingHours> WorkingHours);
}
