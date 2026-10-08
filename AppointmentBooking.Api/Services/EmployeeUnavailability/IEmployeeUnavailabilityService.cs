using AppointmentBooking.Api.DTOs.EmployeeUnavailability.Request;
using AppointmentBooking.Api.DTOs.EmployeeUnavailability.Response;
using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Services.EmployeeAvailabilityBlocks
{
    public interface IEmployeeUnavailabilityService
    {
        Task<EmployeeUnavailabilityResponse> CreateForCurrentEmployeeAsync(
            SetEmployeeUnavailabilityRequest request,
            CancellationToken cancellationToken);

        Task<IReadOnlyCollection<EmployeeUnavailabilityResponse>> GetForCurrentEmployeeAsync(
            CancellationToken cancellationToken);

        Task<EmployeeUnavailabilityResponse> GetForCurrentEmployeeAsync(
            long unavailabilityId,
            CancellationToken cancellationToken);

        Task<EmployeeUnavailabilityResponse> UpdateForCurrentEmployeeAsync(
            long unavailabilityId,
            SetEmployeeUnavailabilityRequest request,
            CancellationToken cancellationToken);

        Task DeleteForCurrentEmployeeAsync(
            long unavailabilityId,
            CancellationToken cancellationToken);

        Task<IReadOnlyCollection<EmployeeUnavailability>> GetOverlappingAsync(
            long employeeId,
            DateTimeOffset startsAt,
            DateTimeOffset endsAt,
            CancellationToken cancellationToken);
    }
}
