using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.EmployeeAvailabilityBlocks
{
    public interface IEmployeeUnavailabilityRepository
    {
        Task<EmployeeUnavailability> CreateAsync(
            EmployeeUnavailability unavailability,
            CancellationToken cancellationToken);

        Task<EmployeeUnavailability?> GetByIdAsync(
            long employeeId,
            long unavailabilityId,
            CancellationToken cancellationToken);

        Task<IReadOnlyCollection<EmployeeUnavailability>> GetByEmployeeIdAsync(
            long employeeId,
            CancellationToken cancellationToken);

        Task<IReadOnlyCollection<EmployeeUnavailability>> GetOverlappingAsync(
            long employeeId,
            DateTimeOffset startsAt,
            DateTimeOffset endsAt,
            CancellationToken cancellationToken);

        Task<EmployeeUnavailability?> UpdateAsync(
            EmployeeUnavailability unavailability,
            CancellationToken cancellationToken);

        Task<bool> DeleteAsync(
            long employeeId,
            long unavailabilityId,
            CancellationToken cancellationToken);
    }
}
