using AppointmentBooking.Api.DTOs.Appointments.Request;
using AppointmentBooking.Api.DTOs.Appointments.Response;
using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.Appointments;

namespace AppointmentBooking.Api.Services.Appointments
{
    public interface IAppointmentService
    {
        Task<AppointmentHoldResponse> CreateHoldAsync(CreateAppointmentHoldRequest request, CancellationToken cancellationToken);

        Task<AppointmentResponse> ConfirmAsync(Guid holdToken, ConfirmHoldAppointment request, CancellationToken cancellationToken);

        Task DeleteHeldAsync(Guid holdToken, CancellationToken cancellationToken);

        Task CancelAsync(long appointmentId, CancellationToken cancellationToken);

        Task CompleteAsync(long appointmentId, CancellationToken cancellationToken);

        Task DeleteExpiredHoldsAsync(CancellationToken cancellationToken);

        Task CompleteExpiredAppointmentsAsync(CancellationToken cancellationToken);

        Task<PagedResponse<AppointmentListItem>> GetAllAsync(GetAppointmentsRequest request, CancellationToken cancellationToken);
    }
}
