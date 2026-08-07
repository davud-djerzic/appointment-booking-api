using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Appointments
{
    public interface IAppointmentRepository
    {
        Task<Appointment> CreateHoldAsync(CreateAppointmentHoldData data, CancellationToken cancellationToken);

        Task<Appointment?> GetByHoldTokenAsync(Guid holdToken, CancellationToken cancellationToken);

        Task<Appointment?> GetByIdAsync(long id, CancellationToken cancellationToken);

        Task<PagedResult<AppointmentListItem>> GetAllAsync(AppointmentSearchCriteria criteria, CancellationToken cancellationToken);

        Task<Appointment> ConfirmHoldAsync(ConfirmAppointmentData data, CancellationToken cancellationToken);

        Task<bool> DeleteHeldAsync(long id, CancellationToken cancellationToken);

        Task<bool> CancelAsync(long appointmentId, CancellationToken cancellationToken);

        Task<bool> CompleteAsync(long appointmentId, CancellationToken cancellationToken);

        Task<int> DeleteExpiredHoldsAsync(CancellationToken cancellationToken);

        Task<int> CompleteExpiredAppointmentsAsync(CancellationToken cancellationToken);
    }
}
