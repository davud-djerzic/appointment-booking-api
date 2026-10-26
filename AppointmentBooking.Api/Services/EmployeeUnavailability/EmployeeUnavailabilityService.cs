using AppointmentBooking.Api.DTOs.EmployeeUnavailability.Request;
using AppointmentBooking.Api.DTOs.EmployeeUnavailability.Response;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Models.Enums;
using AppointmentBooking.Api.Repositories.BookingHolds;
using AppointmentBooking.Api.Repositories.Bookings;
using AppointmentBooking.Api.Repositories.Employees;
using AppointmentBooking.Api.Repositories.EmployeeAvailabilityBlocks;
using AppointmentBooking.Api.Repositories.EmployeeWorkingHoursRepository;
using AppointmentBooking.Api.Services.CurrentUser;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.Services.EmployeeAvailabilityBlocks
{
    public sealed class EmployeeUnavailabilityService(
        IEmployeeUnavailabilityRepository unavailabilityRepository,
        IEmployeeRepository employeeRepository,
        IEmployeeWorkingHoursRepository workingHoursRepository,
        IBookingRepository bookingRepository,
        IBookingHoldRepository bookingHoldRepository,
        ICurrentUserService currentUserService)
        : IEmployeeUnavailabilityService
    {
        public async Task<EmployeeUnavailabilityResponse> CreateForCurrentEmployeeAsync(
            SetEmployeeUnavailabilityRequest request,
            CancellationToken cancellationToken)
        {
            Employee employee = await GetCurrentEmployeeAsync(cancellationToken);
            EmployeeUnavailability unavailability = CreateModel(employee.Id, request);

            await EnsureBreakWithinWorkingHoursAsync(unavailability, cancellationToken);
            await EnsureNoExistingReservationAsync(unavailability, cancellationToken);

            EmployeeUnavailability created = await unavailabilityRepository.CreateAsync(
                unavailability,
                cancellationToken);

            return MapToResponse(created);
        }

        public async Task<IReadOnlyCollection<EmployeeUnavailabilityResponse>> GetForCurrentEmployeeAsync(
            CancellationToken cancellationToken)
        {
            Employee employee = await GetCurrentEmployeeAsync(cancellationToken);

            IReadOnlyCollection<EmployeeUnavailability> items =
                await unavailabilityRepository.GetByEmployeeIdAsync(
                    employee.Id,
                    cancellationToken);

            return items.Select(MapToResponse).ToArray();
        }

        public async Task<EmployeeUnavailabilityResponse> GetForCurrentEmployeeAsync(
            long unavailabilityId,
            CancellationToken cancellationToken)
        {
            Employee employee = await GetCurrentEmployeeAsync(cancellationToken);

            EmployeeUnavailability? item =
                await unavailabilityRepository.GetByIdAsync(
                    employee.Id,
                    unavailabilityId,
                    cancellationToken);

            return item is null
                ? throw new NotFoundException(
                    $"Availability block '{unavailabilityId}' was not found.")
                : MapToResponse(item);
        }

        public async Task<EmployeeUnavailabilityResponse> UpdateForCurrentEmployeeAsync(
            long unavailabilityId,
            SetEmployeeUnavailabilityRequest request,
            CancellationToken cancellationToken)
        {
            Employee employee = await GetCurrentEmployeeAsync(cancellationToken);

            EmployeeUnavailability? existing =
                await unavailabilityRepository.GetByIdAsync(
                    employee.Id,
                    unavailabilityId,
                    cancellationToken);

            if (existing is null)
            {
                throw new NotFoundException(
                    $"Availability block '{unavailabilityId}' was not found.");
            }

            EmployeeUnavailability requestData = CreateModel(
                employee.Id,
                request);

            EmployeeUnavailability update = new()
            {
                Id = existing.Id,
                EmployeeId = requestData.EmployeeId,
                Type = requestData.Type,
                StartsAt = requestData.StartsAt,
                EndsAt = requestData.EndsAt,
                IsAllDay = requestData.IsAllDay,
                Note = requestData.Note,
                CreatedAt = existing.CreatedAt
            };

            await EnsureBreakWithinWorkingHoursAsync(update, cancellationToken);
            await EnsureNoExistingReservationAsync(update, cancellationToken);

            EmployeeUnavailability? updated =
                await unavailabilityRepository.UpdateAsync(
                    update,
                    cancellationToken);

            return updated is null
                ? throw new NotFoundException(
                    $"Availability block '{unavailabilityId}' was not found.")
                : MapToResponse(updated);
        }

        public async Task DeleteForCurrentEmployeeAsync(
            long unavailabilityId,
            CancellationToken cancellationToken)
        {
            Employee employee = await GetCurrentEmployeeAsync(cancellationToken);

            bool deleted = await unavailabilityRepository.DeleteAsync(
                employee.Id,
                unavailabilityId,
                cancellationToken);

            if (!deleted)
            {
                throw new NotFoundException(
                    $"Availability block '{unavailabilityId}' was not found.");
            }
        }

        public Task<IReadOnlyCollection<EmployeeUnavailability>> GetOverlappingAsync(
            long employeeId,
            DateTimeOffset startsAt,
            DateTimeOffset endsAt,
            CancellationToken cancellationToken)
        {
            return unavailabilityRepository.GetOverlappingAsync(
                employeeId,
                startsAt,
                endsAt,
                cancellationToken);
        }

        private async Task<Employee> GetCurrentEmployeeAsync(
            CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByUserAccountIdAsync(
                currentUserService.UserAccountId,
                cancellationToken);

            if (employee is null)
            {
                throw new NotFoundException(
                    "Employee profile for the current account was not found.");
            }

            if (!employee.IsActive)
            {
                throw new ConflictException(
                    "Inactive employees cannot manage availability blocks.");
            }

            return employee;
        }

        private async Task EnsureNoExistingReservationAsync(
            EmployeeUnavailability unavailability,
            CancellationToken cancellationToken)
        {
            bool hasScheduledBooking =
                await bookingRepository.HasScheduledOverlapAsync(
                    unavailability.EmployeeId,
                    unavailability.StartsAt,
                    unavailability.EndsAt,
                    cancellationToken);

            if (hasScheduledBooking)
            {
                throw new ConflictException(
                    "This period overlaps an existing booking. Resolve the booking before adding the block.");
            }

            IReadOnlyList<BookingHold> activeHolds =
                await bookingHoldRepository.GetActiveForEmployeeAsync(
                    unavailability.EmployeeId,
                    unavailability.StartsAt,
                    unavailability.EndsAt,
                    cancellationToken);

            if (activeHolds.Count > 0)
            {
                throw new ConflictException(
                    "This period overlaps an active booking hold. Try again after the hold expires or is cancelled.");
            }
        }

        private async Task EnsureBreakWithinWorkingHoursAsync(
            EmployeeUnavailability unavailability,
            CancellationToken cancellationToken)
        {
            if (unavailability.Type != EmployeeUnavailabilityType.Break)
            {
                return;
            }

            DateTimeOffset localStartsAt = TimeZoneInfo.ConvertTime(
                unavailability.StartsAt,
                AppointmentBooking.Api.Time.SalonTimeZone.Info);
            DateTimeOffset localEndsAt = TimeZoneInfo.ConvertTime(
                unavailability.EndsAt,
                AppointmentBooking.Api.Time.SalonTimeZone.Info);

            if (localStartsAt.Date != localEndsAt.Date)
            {
                throw new ValidationException("Privremena pauza mora početi i završiti istog dana.");
            }

            int isoDayOfWeek = localStartsAt.DayOfWeek == DayOfWeek.Sunday
                ? 7
                : (int)localStartsAt.DayOfWeek;
            IReadOnlyCollection<EmployeeWorkingHours> workingHours =
                await workingHoursRepository.GetByEmployeeIdAsync(
                    unavailability.EmployeeId,
                    cancellationToken);

            TimeOnly breakStartsAt = TimeOnly.FromDateTime(localStartsAt.DateTime);
            TimeOnly breakEndsAt = TimeOnly.FromDateTime(localEndsAt.DateTime);
            bool isWithinWorkingHours = workingHours.Any(hours =>
                (int)hours.DayOfWeek == isoDayOfWeek &&
                hours.StartsAt <= breakStartsAt &&
                hours.EndsAt >= breakEndsAt);

            if (!isWithinWorkingHours)
            {
                throw new ConflictException(
                    "Privremena pauza mora biti u cijelosti unutar radnog vremena zaposlenog za izabrani dan.");
            }
        }

        private static EmployeeUnavailability CreateModel(
            long employeeId,
            SetEmployeeUnavailabilityRequest request)
        {
            Validator.ValidateObject(
                request,
                new ValidationContext(request),
                validateAllProperties: true);

            DateTimeOffset startsAt = TruncateToMinute(request.StartsAt);
            DateTimeOffset endsAt = TruncateToMinute(request.EndsAt);

            if (endsAt <= startsAt)
            {
                throw new ValidationException(
                    "Availability block must be at least one minute long.");
            }

            if (startsAt <= DateTimeOffset.UtcNow)
            {
                throw new ValidationException(
                    "Availability blocks must start in the future.");
            }

            return new EmployeeUnavailability
            {
                EmployeeId = employeeId,
                Type = request.Type,
                StartsAt = startsAt,
                EndsAt = endsAt,
                IsAllDay = request.IsAllDay,
                Note = string.IsNullOrWhiteSpace(request.Note)
                    ? null
                    : request.Note.Trim()
            };
        }

        private static DateTimeOffset TruncateToMinute(DateTimeOffset value)
        {
            DateTimeOffset utcValue = value.ToUniversalTime();

            return new DateTimeOffset(
                utcValue.Year,
                utcValue.Month,
                utcValue.Day,
                utcValue.Hour,
                utcValue.Minute,
                0,
                TimeSpan.Zero);
        }

        private static EmployeeUnavailabilityResponse MapToResponse(
            EmployeeUnavailability unavailability)
        {
            return new EmployeeUnavailabilityResponse(
                unavailability.Id,
                unavailability.EmployeeId,
                unavailability.Type,
                unavailability.StartsAt,
                unavailability.EndsAt,
                unavailability.IsAllDay,
                unavailability.Note,
                unavailability.CreatedAt);
        }
    }
}
