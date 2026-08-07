using AppointmentBooking.Api.Configuration;
using AppointmentBooking.Api.DTOs.Appointments.Request;
using AppointmentBooking.Api.DTOs.Appointments.Response;
using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.Appointments;
using AppointmentBooking.Api.Repositories.Employees;
using AppointmentBooking.Api.Repositories.EmployeeServices;
using AppointmentBooking.Api.Repositories.Services;
using AppointmentBooking.Api.Services.EmployeeServices;
using Microsoft.Extensions.Options;

namespace AppointmentBooking.Api.Services.Appointments
{
    public class AppointmentService(IAppointmentRepository appointmentRepository, IEmployeeRepository employeeRepository, IBookableServiceRepository bookableServiceRepository, IEmployeeServiceRepository assignmentRepository, IOptions<AppointmentOptions> options, TimeProvider timeProvider, ILogger<AppointmentService> logger) : IAppointmentService
    {
        private readonly AppointmentOptions appointmentOptions = options.Value;
        private readonly TimeProvider timeProvider = timeProvider;

        public async Task<AppointmentHoldResponse> CreateHoldAsync(CreateAppointmentHoldRequest request, CancellationToken cancellationToken)
        {
            DateTimeOffset now = timeProvider.GetUtcNow();

            if (request.StartsAt <= now)
            {
                throw new ConflictException(
                    "Appointments cannot be created in the past.");
            }

            if (request.StartsAt >= now.AddMonths(6))
            {
                throw new ConflictException(
                    "Appointments cannot be booked more than 6 months in advance.");
            }

            Employee? employee = await employeeRepository.GetByIdAsync(request.EmployeeId, cancellationToken);
            if (employee is null) throw new NotFoundException($"Employee with ID '{request.EmployeeId}' was not found.");
            if (!employee.IsActive) throw new ConflictException($"Employee with ID '{request.EmployeeId}' is inactive.");

            BookableService? service = await bookableServiceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
            if (service is null) throw new NotFoundException($"Service with ID '{request.ServiceId}' was not found.");
            if (!service.IsActive) throw new ConflictException($"Service with ID '{request.EmployeeId}' is inactive.");

            EmployeeServiceAssignemnt? assignment = await assignmentRepository.GetAsync(request.EmployeeId, request.ServiceId, cancellationToken);
            if (assignment is null || !assignment.IsActive) throw new ConflictException($"Employee '{request.EmployeeId}' does not provide service '{request.ServiceId}'.");

            Guid holdToken = Guid.NewGuid();

            DateTimeOffset holdExpiresAt = timeProvider.GetUtcNow().AddMinutes(appointmentOptions.HoldDurationInMinutes);

            DateTimeOffset endsAt = request.StartsAt.AddMinutes(service.DurationMinutes);

            CreateAppointmentHoldData data = new(
                request.EmployeeId,
                request.ServiceId,
                request.StartsAt,
                endsAt,
                holdToken,
                holdExpiresAt);

            Appointment appointment = await appointmentRepository.CreateHoldAsync(data, cancellationToken);

            return new AppointmentHoldResponse(appointment.Id, appointment.HoldToken!.Value, appointment.HoldExpiresAt!.Value);
        }

        public async Task<AppointmentResponse> ConfirmAsync(Guid holdToken, ConfirmHoldAppointment request, CancellationToken cancellationToken)
        {
            Appointment? appointment = await appointmentRepository.GetByHoldTokenAsync(holdToken, cancellationToken);
            if (appointment is null) throw new NotFoundException($"Appointment with hold token '{holdToken}' was not found.");
            if (appointment.Status != AppointmentStatus.Held) throw new ConflictException($"Appointment with hold token '{holdToken}' is not in a hold state.");
            if (appointment.HoldExpiresAt is null || appointment.HoldExpiresAt <= timeProvider.GetUtcNow()) throw new ConflictException("The reservation hold has expired.");

            ConfirmAppointmentData data = new(
                appointment.Id,
                request.CustomerFirstName,
                request.CustomerLastName,
                request.CustomerEmail,
                request.CustomerPhone,
                request.Notes);
            
            Appointment confirmedAppointment = await appointmentRepository.ConfirmHoldAsync(data, cancellationToken);
            
            return MapToResponse(confirmedAppointment);
        }

        public async Task DeleteHeldAsync(Guid holdToken, CancellationToken cancellationToken)
        {
            Appointment? appointment = await appointmentRepository.GetByHoldTokenAsync(holdToken, cancellationToken);
            if (appointment is null) throw new NotFoundException($"Appointment with hold token '{holdToken}' was not found.");
            if (appointment.Status != AppointmentStatus.Held) throw new ConflictException("Only held appointments can be deleted.");
            bool deleted = await appointmentRepository.DeleteHeldAsync(appointment.Id, cancellationToken);
            if (!deleted) throw new ConflictException($"Failed to delete appointment with hold token '{holdToken}'.");
        }

        public async Task CancelAsync(long appointmentId, CancellationToken cancellationToken)
        {
            Appointment? appointment = await appointmentRepository.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment is null) throw new NotFoundException($"Appointment with ID '{appointmentId}' was not found.");
            if (appointment.Status != AppointmentStatus.Scheduled) throw new ConflictException("Only scheduled appointments can be canceled.");

            bool deleted = await appointmentRepository.CancelAsync(appointment.Id, cancellationToken);
            if (!deleted) throw new ConflictException($"Failed to cancel appointment with id '{appointmentId}'.");
        }

        public async Task CompleteAsync(long appointmentId, CancellationToken cancellationToken)
        {
            Appointment? appointment = await appointmentRepository.GetByIdAsync(appointmentId, cancellationToken);
            if (appointment is null) throw new NotFoundException($"Appointment with ID '{appointmentId}' was not found.");
            if (appointment.Status != AppointmentStatus.Scheduled) throw new ConflictException("Only scheduled appointments can be completed.");
            
            bool deleted = await appointmentRepository.CompleteAsync(appointment.Id, cancellationToken);
            if (!deleted) throw new ConflictException($"Failed to complete appointment with id '{appointmentId}'.");
        }

        public async Task DeleteExpiredHoldsAsync(CancellationToken cancellationToken)
        {
            int deletedCount = await appointmentRepository.DeleteExpiredHoldsAsync(cancellationToken);

            if (deletedCount > 0)
            {
                logger.LogInformation("Deleted {DeletedCount} expired appointment holds.", deletedCount);
            }
        }

        public async Task CompleteExpiredAppointmentsAsync(CancellationToken cancellationToken)
        {
            int completedCount = await appointmentRepository.CompleteExpiredAppointmentsAsync(cancellationToken);
            if (completedCount > 0)
            {
                logger.LogInformation( "Automatic appointment completion finished. {Count} appointment(s) completed.", completedCount);
            }
        }

        public async Task<PagedResponse<AppointmentListItem>> GetAllAsync(GetAppointmentsRequest request, CancellationToken cancellationToken)
        {
            AppointmentSearchCriteria criteria = new AppointmentSearchCriteria
            {
                EmployeeId = request.EmployeeId,
                ServiceId = request.ServiceId,
                Status = request.Status,
                Date = request.Date,
                Page = request.Page,
                PageSize = request.PageSize
            };
            var result = await appointmentRepository.GetAllAsync(criteria, cancellationToken);

            return new PagedResponse<AppointmentListItem>
            {
                Items = result.Items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalCount = result.TotalCount
            };

        }

        private static AppointmentResponse MapToResponse(Appointment appointment)
        {
            return new AppointmentResponse(
                appointment.Id,
                appointment.EmployeeId,
                appointment.ServiceId,
                appointment.CustomerFirstName,
                appointment.CustomerLastName,
                appointment.CustomerEmail,
                appointment.CustomerPhone,
                appointment.StartsAt,
                appointment.EndsAt,
                appointment.Status,
                appointment.Notes, 
                appointment.CreatedAt,
                appointment.UpdatedAt);
        }

    }
}
