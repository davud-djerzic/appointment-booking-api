using AppointmentBooking.Api.Configuration;
using AppointmentBooking.Api.DTOs.Appointments.Response;
using AppointmentBooking.Api.DTOs.BookingHolds.Response;
using AppointmentBooking.Api.DTOs.Bookings.Request;
using AppointmentBooking.Api.DTOs.Bookings.Response;
using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.DTOs.EmployeeServices.Responses;
using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Response;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Models.Enums;
using AppointmentBooking.Api.Repositories.BookingHolds;
using AppointmentBooking.Api.Repositories.Bookings;
using AppointmentBooking.Api.Repositories.Customers;
using AppointmentBooking.Api.Repositories.Employees;
using AppointmentBooking.Api.Repositories.EmployeeServices;
using AppointmentBooking.Api.Repositories.Services;
using AppointmentBooking.Api.Repositories.UserAccounts;
using AppointmentBooking.Api.Services.CurrentUser;
using AppointmentBooking.Api.Services.EmployeeWorkingHoursService;
using AppointmentBooking.Api.Services.EmployeeAvailabilityBlocks;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.Services.Bookings
{
    public sealed class BookingService(IBookingRepository bookingRepository, IBookingHoldRepository bookingHoldRepository, IEmployeeRepository employeeRepository, IBookableServiceRepository bookableServiceRepository,
        IEmployeeServiceRepository employeeServiceRepository, ICustomerRepository customerRepository, ICurrentUserService currentUser, IUserAccountRepository userAccountRepository,IEmployeeWorkingHoursService employeeWorkingHoursService, ILogger<BookingService> logger, IOptions<BookingOptions> options, IEmployeeUnavailabilityService employeeUnavailabilityService) : IBookingService
    {
        private readonly BookingOptions bookingOptions = options.Value;
        public async Task<BookingHoldResponse> CreateHoldAsync(CreateBookingRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Customer customer = await GetActiveCustomerAsync(cancellationToken);

            DateTimeOffset requestedStartsAt = request.StartsAt;
            if (requestedStartsAt <= DateTimeOffset.UtcNow) throw new ValidationException("Booking start time must be in the future.");

            EmployeeBookingContext employeeContext = await GetBookableEmployeeAsync(request.EmployeeId, cancellationToken);

            long[] serviceIds = ValidateServiceIds(request.ServiceIds);

            HoldSchedule schedule = await BuildHoldScheduleAsync(request.EmployeeId, requestedStartsAt, serviceIds, cancellationToken);
            if (schedule.EndsAt <= requestedStartsAt) throw new ValidationException("Booking end time must be after the start time.");
            
            bool isWithinWorkingHours = await employeeWorkingHoursService.IsWithinWorkingHoursAsync(request.EmployeeId, requestedStartsAt, schedule.EndsAt, cancellationToken);
            if (!isWithinWorkingHours) throw new ConflictException("The selected time is outside the employee's working hours.");

            IReadOnlyCollection<EmployeeUnavailability> unavailability =
                await employeeUnavailabilityService.GetOverlappingAsync(
                    request.EmployeeId,
                    requestedStartsAt,
                    schedule.EndsAt,
                    cancellationToken);

            if (unavailability.Count > 0)
            {
                throw new ConflictException(
                    "The selected time overlaps an employee break or leave.");
            }

            DateTimeOffset startsAt = requestedStartsAt.ToUniversalTime();
            DateTimeOffset endsAt = schedule.EndsAt.ToUniversalTime();

            bool hasScheduledOverlap = await bookingRepository.HasScheduledOverlapAsync(request.EmployeeId, startsAt, endsAt, cancellationToken);
            if (hasScheduledOverlap) throw new ConflictException("The employee is already booked for the selected time.");

            TimeSpan holdTtl = TimeSpan.FromMinutes(bookingOptions.HoldDurationInMinutes);

            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(holdTtl);
            BookingHold hold = new()
            {
                HoldToken = Guid.NewGuid(),
                CustomerId = customer.Id,
                EmployeeId = request.EmployeeId,
                StartsAt = startsAt,
                EndsAt = endsAt,
                Services = schedule.Services,
                Notes = string.IsNullOrWhiteSpace(request.Notes)
                    ? null
                    : request.Notes.Trim(),
                ExpiresAt = expiresAt
            };

            bool holdCreated = await bookingHoldRepository.TryCreateAsync(hold, holdTtl, cancellationToken);
            if (!holdCreated) throw new ConflictException("The employee is already temporarily reserved for the selected time.");

            IReadOnlyCollection<EmployeeUnavailability> currentUnavailability =
                await employeeUnavailabilityService.GetOverlappingAsync(
                    request.EmployeeId,
                    hold.StartsAt,
                    hold.EndsAt,
                    cancellationToken);

            if (currentUnavailability.Count > 0)
            {
                await DeleteHoldSafelyAsync(hold);

                throw new ConflictException(
                    "The selected time overlaps an employee break or leave.");
            }

            BookingHoldServiceResponse[] serviceResponses =
                hold.Services
                    .Select(service => new BookingHoldServiceResponse(
                        service.ServiceId,
                        service.Name,
                        service.DurationMinutes,
                        service.Price,
                        service.StartsAt,
                        service.EndsAt))
                    .ToArray();

            return new BookingHoldResponse(
                hold.HoldToken,
                new EmployeeSummaryResponse(
                    employeeContext.Employee.Id,
                    employeeContext.UserAccount.FirstName,
                    employeeContext.UserAccount.LastName),
                hold.StartsAt,
                hold.EndsAt,
                schedule.TotalDurationMinutes,
                schedule.TotalPrice,
                serviceResponses,
                hold.ExpiresAt);
        }

        public async Task<BookingResponse> ConfirmAsync(Guid holdToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Customer customer = await GetActiveCustomerAsync(cancellationToken);

            BookingHold? hold = await bookingHoldRepository.GetAsync(holdToken, cancellationToken);
            if (hold is null) throw new ConflictException("The booking hold has expired or no longer exists.");
            

            if (hold.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                await DeleteHoldSafelyAsync(hold);

                throw new ConflictException("The booking hold has expired or no longer exists.");
            }

            if (hold.CustomerId != customer.Id) throw new UnauthorizedException("The booking hold does not belong to the current customer.");

            await GetBookableEmployeeAsync(hold.EmployeeId, cancellationToken);

            IReadOnlyCollection<EmployeeUnavailability> unavailability =
                await employeeUnavailabilityService.GetOverlappingAsync(
                    hold.EmployeeId,
                    hold.StartsAt,
                    hold.EndsAt,
                    cancellationToken);

            if (unavailability.Count > 0)
            {
                await DeleteHoldSafelyAsync(hold);

                throw new ConflictException(
                    "The held time overlaps an employee break or leave.");
            }

            List<Appointment> appointments =
                hold.Services
                    .Select(service =>
                        new Appointment
                        {
                            ServiceId = service.ServiceId,
                            ServiceNameAtBooking = service.Name,
                            DurationMinutesAtBooking = service.DurationMinutes,
                            PriceAtBooking = service.Price,
                            StartsAt = service.StartsAt,
                            EndsAt = service.EndsAt,
                            Status = AppointmentStatus.Scheduled
                        })
                    .ToList();

            bool hasScheduledOverlap = await bookingRepository.HasScheduledOverlapAsync(hold.EmployeeId, hold.StartsAt, hold.EndsAt, cancellationToken);
            if (hasScheduledOverlap)
            {
                await DeleteHoldSafelyAsync(hold);

                throw new ConflictException(
                    "The employee is already booked for the selected time.");
            }

            Booking booking = new()
            {
                CustomerId = customer.Id,
                EmployeeId = hold.EmployeeId,
                StartsAt = hold.StartsAt,
                EndsAt = hold.EndsAt,
                Status = BookingStatus.Scheduled,
                Notes = hold.Notes
            };

            try
            {
                BookingCreationResult result = await bookingRepository.CreateAsync(booking, appointments, cancellationToken);

                await DeleteHoldSafelyAsync(hold);

                return MapToResponse(result.Booking, result.Appointments);
            }
            catch
            {
                await DeleteHoldSafelyAsync(hold);

                throw;
            }
        }

        public async Task<BookingDetailsResponse> GetByIdAsync(long bookingId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Customer customer =await GetActiveCustomerAsync(cancellationToken);

            BookingReadResult? result = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
            if (result is null) throw new NotFoundException($"Booking with ID '{bookingId}' was not found.");
            if (result.Booking.CustomerId != customer.Id) throw new NotFoundException($"Booking with ID '{bookingId}' was not found.");

            int totalDurationMinutes = result.Appointments.Sum(appointment => appointment.DurationMinutesAtBooking);
            decimal totalPrice = result.Appointments.Sum(appointment => appointment.PriceAtBooking);
            
            AppointmentResponse[] appointmentResponses = result.Appointments.Select(MapAppointmentToResponse).ToArray();

            return new BookingDetailsResponse(
                result.Booking.Id,
                new EmployeeSummaryResponse(
                    result.Booking.EmployeeId,
                    result.EmployeeFirstName,
                    result.EmployeeLastName),
                result.Booking.StartsAt,
                result.Booking.EndsAt,
                totalDurationMinutes,
                totalPrice,
                result.Booking.Status,
                result.Booking.Notes,
                result.Booking.CreatedAt,
                result.Booking.CompletedAt,
                result.Booking.CompletionSource,
                appointmentResponses);
        }

        public async Task<PagedResponse<BookingSummaryResponse>> GetMyBookingsAsync(GetBookingsQuery query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Customer? customer = await GetActiveCustomerAsync(cancellationToken);

            BookingSearchCriteria criteria = new()
            {
                Status = query.Status,
                From = query.From,
                To = query.To,
                Page = query.Page,
                PageSize = query.PageSize
            };

            PagedResult<BookingListItem> result = await bookingRepository.GetCustomerBookingsAsync(customer.Id, criteria, cancellationToken);

            BookingSummaryResponse[] items =
                result.Items
                    .Select(item =>
                        new BookingSummaryResponse(
                            item.Id,
                            new EmployeeSummaryResponse(
                                item.EmployeeId,
                                item.EmployeeFirstName,
                                item.EmployeeLastName),
                            item.StartsAt,
                            item.EndsAt,
                            item.TotalDurationMinutes,
                            item.TotalPrice,
                            item.Status,
                            item.Notes,
                            item.CreatedAt))
                    .ToArray();

            return new PagedResponse<BookingSummaryResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = result.TotalCount
            };
        }

        public async Task CancelHoldAsync(Guid holdToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Customer customer = await GetActiveCustomerAsync(cancellationToken);

            BookingHold? hold = await bookingHoldRepository.GetAsync(holdToken, cancellationToken);
            if (hold is null) throw new ConflictException("The booking hold was not found.");
            if (hold.CustomerId != customer.Id) throw new UnauthorizedException("The booking hold does not belong to the current customer.");

            if (hold.ExpiresAt <= DateTimeOffset.UtcNow)
            {
                await DeleteHoldSafelyAsync(hold);

                throw new ConflictException(
                    "The booking hold has expired or no longer exists.");
            }

            await bookingHoldRepository.DeleteAsync(hold,cancellationToken);
        }

        public async Task CancelBookingAsync(long bookingId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (currentUser.Role)
            {
                case UserRole.Customer:
                    await CancelBookingAsCustomerAsync(bookingId, cancellationToken);
                    break;

                case UserRole.Employee:
                    await CancelBookingAsEmployeeAsync(bookingId, cancellationToken);
                    break;

                case UserRole.Admin:
                    await CancelBookingAsAdminAsync(bookingId, cancellationToken);
                    break;

                default:
                    throw new UnauthorizedException(
                        "The current user is not authorized to cancel bookings.");

            }
        }

        public async Task CompleteBookingAsync(long bookingId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (currentUser.Role)
            {
                case UserRole.Employee:
                    await CompleteBookingAsEmployeeAsync(bookingId, cancellationToken);
                    break;

                case UserRole.Admin:
                    await CompleteBookingAsAdminAsync(bookingId, cancellationToken);
                    break;

                default:
                    throw new UnauthorizedException("The current user is not authorized to complete bookings.");
            }
        }

        public async Task<PagedResponse<EmployeeBookingSummaryResponse>> GetEmployeeBookingsAsync(GetBookingsQuery query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (currentUser.Role == UserRole.Admin)
            {
                PagedResponse<AdminBookingSummaryResponse> adminBookings =
                    await GetAdminBookingsAsync(
                        new GetAdminBookingsQuery
                        {
                            From = query.From.HasValue
                                ? DateOnly.FromDateTime(query.From.Value.DateTime)
                                : null,
                            To = query.To.HasValue
                                ? DateOnly.FromDateTime(query.To.Value.DateTime)
                                : null,
                            Status = query.Status,
                            Page = query.Page,
                            PageSize = query.PageSize
                        },
                        cancellationToken);

                EmployeeBookingSummaryResponse[] adminItems =
                    adminBookings.Items
                        .Select(booking =>
                            new EmployeeBookingSummaryResponse(
                                booking.Id,
                                booking.Customer,
                                booking.StartsAt,
                                booking.EndsAt,
                                booking.TotalDurationMinutes,
                                booking.TotalPrice,
                                booking.Status,
                                booking.Notes,
                                booking.CreatedAt))
                        .ToArray();

                return new PagedResponse<EmployeeBookingSummaryResponse>
                {
                    Items = adminItems,
                    Page = adminBookings.Page,
                    PageSize = adminBookings.PageSize,
                    TotalCount = adminBookings.TotalCount
                };
            }

            Employee employee = await GetActiveEmployeeAsync(cancellationToken);

            BookingSearchCriteria criteria = new()
            {
                Status = query.Status,
                From = query.From,
                To = query.To,
                Page = query.Page,
                PageSize = query.PageSize
            };

            PagedResult<EmployeeBookingListItem> result = await bookingRepository.GetEmployeeBookingsAsync(employee.Id, criteria, cancellationToken);

            EmployeeBookingSummaryResponse[] items =
                result.Items
                    .Select(item =>
                        new EmployeeBookingSummaryResponse(
                            item.Id,
                            new CustomerSummaryResponse(
                                item.CustomerId,
                                item.CustomerFirstName,
                                item.CustomerLastName,
                                item.CustomerEmail,
                                item.CustomerPhone),
                            item.StartsAt,
                            item.EndsAt,
                            item.TotalDurationMinutes,
                            item.TotalPrice,
                            item.Status,
                            item.Notes,
                            item.CreatedAt))
                    .ToArray();

            return new PagedResponse<EmployeeBookingSummaryResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = result.TotalCount
            };
        }

        public async Task<EmployeeBookingDetailsResponse> GetEmployeeBookingByIdAsync(long bookingId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Employee? employee = currentUser.Role == UserRole.Admin
                ? null
                : await GetActiveEmployeeAsync(cancellationToken);

            EmployeeBookingDetailsReadResult? result =
                await bookingRepository.GetEmployeeBookingDetailsAsync(
                    employee?.Id,
                    bookingId,
                    cancellationToken);
            if (result is null) throw new NotFoundException($"Booking with ID '{bookingId}' was not found.");

            int totalDurationMinutes = result.Appointments.Sum(appointment => appointment.DurationMinutesAtBooking);
            decimal totalPrice = result.Appointments.Sum(appointment => appointment.PriceAtBooking);

            AppointmentResponse[] appointments =
                result.Appointments
                    .Select(appointment =>
                        new AppointmentResponse(
                            appointment.Id,
                            appointment.BookingId,
                            appointment.ServiceId,
                            appointment.ServiceNameAtBooking,
                            appointment.DurationMinutesAtBooking,
                            appointment.PriceAtBooking,
                            appointment.StartsAt,
                            appointment.EndsAt,
                            appointment.Status))
                    .ToArray();

            return new EmployeeBookingDetailsResponse(
                result.Booking.Id,
                new CustomerSummaryResponse(
                    result.CustomerId,
                    result.CustomerFirstName,
                    result.CustomerLastName,
                    result.CustomerEmail,
                    result.CustomerPhone),
                result.Booking.StartsAt,
                result.Booking.EndsAt,
                totalDurationMinutes,
                totalPrice,
                result.Booking.Status,
                result.Booking.Notes,
                result.Booking.CreatedAt,
                result.Booking.CompletedAt,
                result.Booking.CompletionSource,
                appointments);
        }

        public async Task<EmployeeBookingDetailsResponse> CreateStaffBookingAsync(
            CreateStaffBookingRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(request.CustomerFirstName) ||
                string.IsNullOrWhiteSpace(request.CustomerLastName))
            {
                throw new ValidationException("Customer first and last name are required.");
            }

            Employee employee;
            if (currentUser.Role == UserRole.Employee)
            {
                employee = await GetActiveEmployeeAsync(cancellationToken);

                if (request.EmployeeId.HasValue && request.EmployeeId.Value != employee.Id)
                {
                    throw new UnauthorizedException("An employee can only create bookings for themselves.");
                }
            }
            else if (currentUser.Role == UserRole.Admin)
            {
                if (!request.EmployeeId.HasValue || request.EmployeeId.Value <= 0)
                {
                    throw new ValidationException("An employee must be selected.");
                }

                EmployeeBookingContext employeeContext = await GetBookableEmployeeAsync(
                    request.EmployeeId.Value,
                    cancellationToken);
                employee = employeeContext.Employee;
            }
            else
            {
                throw new UnauthorizedException("The current user is not authorized to create staff bookings.");
            }

            DateTimeOffset startsAt = request.StartsAt.ToUniversalTime();
            if (startsAt <= DateTimeOffset.UtcNow)
            {
                throw new ValidationException("Booking start time must be in the future.");
            }

            long[] serviceIds = ValidateServiceIds(request.ServiceIds);
            HoldSchedule schedule = await BuildHoldScheduleAsync(
                employee.Id,
                startsAt,
                serviceIds,
                cancellationToken);

            if (!await employeeWorkingHoursService.IsWithinWorkingHoursAsync(
                    employee.Id,
                    startsAt,
                    schedule.EndsAt,
                    cancellationToken))
            {
                throw new ConflictException("The selected time is outside the employee's working hours.");
            }

            IReadOnlyCollection<EmployeeUnavailability> unavailability =
                await employeeUnavailabilityService.GetOverlappingAsync(
                    employee.Id,
                    startsAt,
                    schedule.EndsAt,
                    cancellationToken);

            if (unavailability.Count > 0)
            {
                throw new ConflictException("The selected time overlaps an employee break or leave.");
            }

            if (await bookingRepository.HasScheduledOverlapAsync(
                    employee.Id,
                    startsAt,
                    schedule.EndsAt,
                    cancellationToken))
            {
                throw new ConflictException("The employee is already booked for the selected time.");
            }

            IReadOnlyList<BookingHold> activeHolds =
                await bookingHoldRepository.GetActiveForEmployeeAsync(
                    employee.Id,
                    startsAt,
                    schedule.EndsAt,
                    cancellationToken);

            if (activeHolds.Any(hold => startsAt < hold.EndsAt && schedule.EndsAt > hold.StartsAt))
            {
                throw new ConflictException("The selected time is temporarily reserved by a customer.");
            }

            Appointment[] appointments = schedule.Services
                .Select(service => new Appointment
                {
                    ServiceId = service.ServiceId,
                    ServiceNameAtBooking = service.Name,
                    DurationMinutesAtBooking = service.DurationMinutes,
                    PriceAtBooking = service.Price,
                    StartsAt = service.StartsAt,
                    EndsAt = service.EndsAt,
                    Status = AppointmentStatus.Scheduled
                })
                .ToArray();

            Booking booking = new()
            {
                CustomerFirstName = request.CustomerFirstName.Trim(),
                CustomerLastName = request.CustomerLastName.Trim(),
                EmployeeId = employee.Id,
                StartsAt = startsAt,
                EndsAt = schedule.EndsAt.ToUniversalTime(),
                Status = BookingStatus.Scheduled,
                Notes = string.IsNullOrWhiteSpace(request.Notes)
                    ? null
                    : request.Notes.Trim()
            };

            BookingCreationResult created = await bookingRepository.CreateAsync(
                booking,
                appointments,
                cancellationToken);

            return await GetEmployeeBookingByIdAsync(
                created.Booking.Id,
                cancellationToken);
        }

        public async Task<BookingAvailabilityResponse> GetAvailabilityAsync(GetBookingAvailabilityQuery query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long employeeId = query.EmployeeId;

            long[] serviceIds = ValidateServiceIds(query.ServiceIds);

            DateOnly date = query.Date;
            DateOnly today = DateOnly.FromDateTime(DateTimeOffset.Now.DateTime);
            if (date < today) throw new ValidationException("Availability cannot be requested for a date in the past.");

            await GetBookableEmployeeAsync(employeeId, cancellationToken);

            int totalDurationMinutes = await GetTotalServiceDurationAsync(employeeId, serviceIds, cancellationToken);

            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                return new BookingAvailabilityResponse(employeeId, date, totalDurationMinutes, Array.Empty<AvailableTimeSlotResponse>());
            }
            
            IReadOnlyCollection<EmployeeWorkingHoursResponse> workingHours = await employeeWorkingHoursService.GetByEmployeeIdAsync(employeeId,cancellationToken);

            WeekDay dayOfWeek = GetWeekDay(date.DayOfWeek);

            EmployeeWorkingHoursResponse[] dailyWorkingHours = workingHours
                .Where(x => x.DayOfWeek == dayOfWeek)
                .OrderBy(x => x.StartsAt)
                .ToArray();

            if (dailyWorkingHours.Length == 0) return new BookingAvailabilityResponse(employeeId, date, totalDurationMinutes, Array.Empty<AvailableTimeSlotResponse>());

            DateTime localDate = date.ToDateTime(TimeOnly.MinValue);
            DateTimeOffset dayStart = new(localDate, TimeZoneInfo.Local.GetUtcOffset(localDate));
            DateTimeOffset dayEnd = dayStart.AddDays(1);

            IReadOnlyList<BookingTimeRange> scheduledBookings = await bookingRepository.GetScheduledTimeRangesAsync(employeeId, dayStart, dayEnd, cancellationToken);
            IReadOnlyList<BookingHold> activeHolds = await bookingHoldRepository.GetActiveForEmployeeAsync(employeeId, dayStart, dayEnd, cancellationToken);
            IReadOnlyCollection<EmployeeUnavailability> unavailability =
                await employeeUnavailabilityService.GetOverlappingAsync(
                    employeeId,
                    dayStart,
                    dayEnd,
                    cancellationToken);

            List<AvailableTimeSlotResponse> availableSlots = new();
            TimeSpan slotInterval = TimeSpan.FromMinutes(bookingOptions.AvailabilitySlotIntervalMinutes);

            DateTimeOffset now = DateTimeOffset.Now;

            foreach (EmployeeWorkingHoursResponse workingHour in dailyWorkingHours)
            {
                DateTime workingStartLocal = date.ToDateTime(workingHour.StartsAt);
                DateTime workingEndLocal = date.ToDateTime(workingHour.EndsAt);

                DateTimeOffset workingStart = new(workingStartLocal, TimeZoneInfo.Local.GetUtcOffset(workingStartLocal));
                DateTimeOffset workingEnd = new(workingEndLocal, TimeZoneInfo.Local.GetUtcOffset(workingEndLocal));

                DateTimeOffset candidateStart = workingStart;

                while(candidateStart.AddMinutes(totalDurationMinutes) <= workingEnd)
                {
                    if (date == today && candidateStart <= now)
                    {
                        candidateStart = candidateStart.Add(slotInterval);
                        continue;
                    }

                    DateTimeOffset candidateEnd = candidateStart.AddMinutes(totalDurationMinutes);
                    bool overlapsUnavailability = unavailability.Any(block =>
                        candidateStart < block.EndsAt &&
                        candidateEnd > block.StartsAt);

                    if (overlapsUnavailability)
                    {
                        candidateStart = candidateStart.Add(slotInterval);
                        continue;
                    }

                    bool hasScheduledOverlap = scheduledBookings.Any(booking => candidateStart < booking.EndsAt && candidateEnd > booking.StartsAt);

                    if (!hasScheduledOverlap)
                    {
                        bool hasActiveHoldOverlap = activeHolds.Any(hold => candidateStart < hold.EndsAt && candidateEnd > hold.StartsAt);
                        if (!hasActiveHoldOverlap)
                        {
                            availableSlots.Add(new AvailableTimeSlotResponse(candidateStart, candidateEnd));
                        }
                    }

                    candidateStart = candidateStart.Add(slotInterval);
                }
            }

            return new BookingAvailabilityResponse(employeeId, date, totalDurationMinutes, availableSlots);
        }

        public async Task<BookingCalendarDaysResponse> GetCalendarDaysAsync(
            GetBookingCalendarDaysQuery query,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await GetBookableEmployeeAsync(query.EmployeeId, cancellationToken);

            IReadOnlyCollection<EmployeeWorkingHoursResponse> workingHours =
                await employeeWorkingHoursService.GetByEmployeeIdAsync(
                    query.EmployeeId,
                    cancellationToken);

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            HashSet<WeekDay> configuredWeekdays = workingHours
                .Select(item => item.DayOfWeek)
                .ToHashSet();

            DateOnly preliminaryFrom = FindWorkingDateBoundary(today, configuredWeekdays, -1, 30);
            DateOnly preliminaryTo = FindWorkingDateBoundary(today, configuredWeekdays, 1, 60);

            DateOnly scanFrom = preliminaryFrom.AddDays(-365);
            DateOnly scanTo = preliminaryTo.AddDays(365);
            DateTime localStart = scanFrom.ToDateTime(TimeOnly.MinValue);
            DateTime localEnd = scanTo.AddDays(1).ToDateTime(TimeOnly.MinValue);
            DateTimeOffset rangeStart = new(localStart, TimeZoneInfo.Local.GetUtcOffset(localStart));
            DateTimeOffset rangeEnd = new(localEnd, TimeZoneInfo.Local.GetUtcOffset(localEnd));

            IReadOnlyCollection<EmployeeUnavailability> unavailability =
                await employeeUnavailabilityService.GetOverlappingAsync(
                    query.EmployeeId,
                    rangeStart,
                    rangeEnd,
                    cancellationToken);

            HashSet<DateOnly> vacationDates = [];
            foreach (EmployeeUnavailability vacation in unavailability.Where(
                item => item.Type == EmployeeUnavailabilityType.Vacation && item.IsAllDay))
            {
                DateOnly vacationStart = DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTime(vacation.StartsAt, TimeZoneInfo.Local).DateTime);
                DateOnly vacationEndExclusive = DateOnly.FromDateTime(
                    TimeZoneInfo.ConvertTime(vacation.EndsAt, TimeZoneInfo.Local).DateTime);

                for (DateOnly vacationDate = vacationStart;
                    vacationDate < vacationEndExclusive;
                    vacationDate = vacationDate.AddDays(1))
                {
                    vacationDates.Add(vacationDate);
                }
            }

            List<DateOnly> candidateDates = [];
            for (DateOnly date = scanFrom; date <= scanTo; date = date.AddDays(1))
            {
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    continue;
                }

                WeekDay dayOfWeek = GetWeekDay(date.DayOfWeek);
                if (configuredWeekdays.Contains(dayOfWeek) && !vacationDates.Contains(date))
                {
                    candidateDates.Add(date);
                }
            }

            List<DateOnly> dates = candidateDates
                .Where(date => date < today)
                .TakeLast(30)
                .Concat(candidateDates.Where(date => date >= today).Take(60))
                .Order()
                .ToList();

            DateOnly responseFrom = dates.Count > 0 ? dates[0] : today;
            DateOnly responseTo = dates.Count > 0 ? dates[^1] : today;

            return new BookingCalendarDaysResponse(
                query.EmployeeId,
                responseFrom,
                responseTo,
                dates);
        }

        private static DateOnly FindWorkingDateBoundary(
            DateOnly today,
            IReadOnlySet<WeekDay> configuredWeekdays,
            int direction,
            int count)
        {
            DateOnly date = direction > 0 ? today.AddDays(-1) : today;
            int found = 0;
            int maxCalendarDays = 366 * 2;

            for (int attempt = 0; attempt < maxCalendarDays; attempt++)
            {
                date = date.AddDays(direction);
                if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    continue;
                }

                if (configuredWeekdays.Contains(GetWeekDay(date.DayOfWeek)) && ++found == count)
                {
                    return date;
                }
            }

            return date;
        }

        public async Task<IReadOnlyList<BookableEmployeeResponse>> GetBookableEmployeesAsync(IReadOnlyCollection<long> serviceIds, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long[] validatedServiceIds =
                ValidateServiceIds(serviceIds);

            return await bookingRepository.GetBookableEmployeesAsync(
                validatedServiceIds,
                cancellationToken);
        }
        public async Task<PagedResponse<AdminBookingSummaryResponse>> GetAdminBookingsAsync(GetAdminBookingsQuery query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AdminBookingSearchCriteria criteria = new()
            {
                Date = query.Date,
                From = query.From,
                To = query.To,
                EmployeeId = query.EmployeeId,
                CustomerId = query.CustomerId,
                Status = query.Status,
                Page = query.Page,
                PageSize = query.PageSize
            };

            PagedResult<AdminBookingListItem> result =
                await bookingRepository.GetAdminBookingsAsync(
                    criteria,
                    cancellationToken);

            AdminBookingSummaryResponse[] items =
                result.Items
                    .Select(booking => new AdminBookingSummaryResponse(
                        booking.Id,
                        new CustomerSummaryResponse(
                            booking.CustomerId,
                            booking.CustomerFirstName,
                            booking.CustomerLastName,
                            booking.CustomerEmail,
                            booking.CustomerPhone),
                        new EmployeeSummaryResponse(
                            booking.EmployeeId,
                            booking.EmployeeFirstName,
                            booking.EmployeeLastName),
                        booking.StartsAt,
                        booking.EndsAt,
                        booking.TotalDurationMinutes,
                        booking.TotalPrice,
                        booking.Status,
                        booking.Notes,
                        booking.CreatedAt))
                    .ToArray();

            return new PagedResponse<AdminBookingSummaryResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = result.TotalCount
            };
        }

        public async Task<QuickAvailabilityResponse> GetQuickAvailabilityAsync(GetQuickAvailabilityQuery query, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BookableService? service = await bookableServiceRepository.GetByIdAsync(query.ServiceId, cancellationToken);
            if (service is null) throw new NotFoundException($"Service with ID '{query.ServiceId}' was not found.");
            if (!service.IsActive) throw new ConflictException($"Service with ID '{query.ServiceId}' is inactive.");

            IEnumerable<ServiceEmployeeResponse> employees = await employeeServiceRepository.GetServiceEmployeesAsync(query.ServiceId, cancellationToken);
            ServiceEmployeeResponse? employee = employees.FirstOrDefault();

            if (employee is null) throw new ConflictException("There are currently no employees available for this service.");
            
            const int maxDaysToSearch = 30;

            DateOnly date = DateOnly.FromDateTime(DateTimeOffset.Now.DateTime);

            for (int dayOffset = 0; dayOffset < maxDaysToSearch; dayOffset++)
            {
                DateOnly candidateDate = date.AddDays(dayOffset);

                BookingAvailabilityResponse availability = await GetAvailabilityAsync(
                    new GetBookingAvailabilityQuery
                    {
                        EmployeeId = employee.Id,
                        ServiceIds = [query.ServiceId],
                        Date = candidateDate
                    },
                    cancellationToken);

                if (availability.Slots.Count == 0)
                {
                    continue;
                }

                return new QuickAvailabilityResponse(
                    employee.Id,
                    service.Id,
                    service.Name,
                    service.DurationMinutes,
                    service.Price,
                    candidateDate,
                    availability.Slots);
            }

            return new QuickAvailabilityResponse(
                    employee.Id,
                    service.Id,
                    service.Name,
                    service.DurationMinutes,
                    service.Price,
                    null,
                    []);
        }
            

        private static WeekDay GetWeekDay(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => WeekDay.Monday,
                DayOfWeek.Tuesday => WeekDay.Tuesday,
                DayOfWeek.Wednesday => WeekDay.Wednesday,
                DayOfWeek.Thursday => WeekDay.Thursday,
                DayOfWeek.Friday => WeekDay.Friday,
                DayOfWeek.Saturday => WeekDay.Saturday,
                DayOfWeek.Sunday => WeekDay.Sunday,
                _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek))
            };
        }

        private async Task<int> GetTotalServiceDurationAsync(long employeeId, IReadOnlyCollection<long> serviceIds,CancellationToken cancellationToken)
        {
            IReadOnlyCollection<BookableService> services =
                await bookableServiceRepository.GetByIdsAsync(
                    serviceIds,
                    cancellationToken);

            IReadOnlyCollection<EmployeeServiceAssignemnt> assignments =
                await employeeServiceRepository.GetByEmployeeAndServiceIdsAsync(
                    employeeId,
                    serviceIds,
                    cancellationToken);

            Dictionary<long, BookableService> servicesById =
                services.ToDictionary(service => service.Id);

            Dictionary<long, EmployeeServiceAssignemnt> assignmentsByServiceId =
                assignments.ToDictionary(assignment => assignment.ServiceId);

            int totalDurationMinutes = 0;

            foreach (long serviceId in serviceIds)
            {
                if (!servicesById.TryGetValue(
                        serviceId,
                        out BookableService? service))
                {
                    throw new NotFoundException(
                        $"Service with ID '{serviceId}' was not found.");
                }

                if (!service.IsActive)
                {
                    throw new ConflictException(
                        $"Service with ID '{serviceId}' is inactive.");
                }

                if (!assignmentsByServiceId.TryGetValue(
                        serviceId,
                        out EmployeeServiceAssignemnt? assignment))
                {
                    throw new ConflictException(
                        $"Employee with ID '{employeeId}' does not provide service with ID '{serviceId}'.");
                }

                if (!assignment.IsActive)
                {
                    throw new ConflictException(
                        $"Employee with ID '{employeeId}' does not currently provide service with ID '{serviceId}'.");
                }

                totalDurationMinutes += service.DurationMinutes;
            }

            return totalDurationMinutes;
        }

        private async Task CancelBookingAsCustomerAsync(long bookingId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Customer customer = await GetActiveCustomerAsync(cancellationToken);

            BookingReadResult? result = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
            if (result is null) throw new NotFoundException($"Booking with ID '{bookingId}' was not found.");

            if (result.Booking.CustomerId != customer.Id) throw new NotFoundException($"Booking with ID '{bookingId}' was not found.");

            ValidateCanBeCancelled(result.Booking, allowAutomaticCompleted: false);
            await CancelBookingInternalAsync(bookingId, cancellationToken);
        }

        private async Task CancelBookingAsEmployeeAsync(long bookingId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Employee? employee = await GetActiveEmployeeAsync(cancellationToken);

            BookingReadResult? result = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
            if (result is null) throw new NotFoundException($"Booking with ID '{bookingId}' was not found.");

            if (result.Booking.EmployeeId != employee.Id) throw new NotFoundException($"Booking with ID '{bookingId}' was not found.");

            ValidateCanBeCancelledByEmployee(result.Booking);

            bool cancelled = await bookingRepository.CancelAndBlockAsEmployeeAsync(
                bookingId,
                employee.Id,
                $"Pauza nakon otkazanog termina #{bookingId}.",
                cancellationToken);

            if (!cancelled)
            {
                throw new ConflictException(
                    "The booking could not be cancelled because its state changed or it is no longer in the future.");
            }
        }

        private async Task CancelBookingAsAdminAsync(long bookingId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BookingReadResult? result = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
            if (result is null) throw new NotFoundException($"Booking with ID '{bookingId}' was not found.");

            ValidateCanBeCancelled(result.Booking, allowAutomaticCompleted: true);
            await CancelBookingInternalAsync(bookingId, cancellationToken);
        }

        private async Task CompleteBookingAsEmployeeAsync(long bookingId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            Employee? employee = await GetActiveEmployeeAsync(cancellationToken);

            BookingReadResult? result =await bookingRepository.GetByIdAsync(bookingId, cancellationToken);
            if (result is null) throw new NotFoundException($"Booking with ID '{bookingId}' was not found.");
            if (result.Booking.EmployeeId != employee.Id) throw new NotFoundException(  $"Booking with ID '{bookingId}' was not found.");
            

            ValidateCanBeCompleted(result.Booking);

            await CompleteBookingInternalAsync(bookingId, cancellationToken);
        }

        private async Task CompleteBookingAsAdminAsync(long bookingId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            BookingReadResult? result = await bookingRepository.GetByIdAsync(bookingId, cancellationToken);

            if (result is null) throw new NotFoundException($"Booking with ID '{bookingId}' was not found.");
            

            ValidateCanBeCompleted(result.Booking);

            await CompleteBookingInternalAsync(bookingId, cancellationToken);
        }

        private static void ValidateCanBeCompleted(Booking booking)
        {
            if (booking.Status == BookingStatus.Completed) throw new ConflictException("The booking is already completed.");
            if (booking.Status == BookingStatus.Cancelled) throw new ConflictException("A cancelled booking cannot be completed.");
            if (booking.Status != BookingStatus.Scheduled)throw new ConflictException("The booking cannot be completed in its current state.");
            if (booking.EndsAt > DateTimeOffset.UtcNow) throw new ConflictException("The booking cannot be completed before the appointment has ended."); 
        }

        private async Task CompleteBookingInternalAsync(long bookingId, CancellationToken cancellationToken)
        {
            bool completed = await bookingRepository.CompleteAsync(bookingId, cancellationToken);
            if (!completed) throw new ConflictException("The booking could not be completed because its state changed or the appointment has not ended.");
        }

        private async Task<Employee> GetActiveEmployeeAsync(CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByUserAccountIdAsync(currentUser.UserAccountId, cancellationToken);
            if (employee is null) throw new UnauthorizedException("The employee profile was not found.");
            if (!employee.IsActive) throw new UnauthorizedException("The employee account is inactive.");

            UserAccount? userAccount = await userAccountRepository.GetByIdAsync(employee.UserAccountId, cancellationToken);
            if (userAccount is null)throw new UnauthorizedException("The employee account was not found.");
            if (!userAccount.IsActive) throw new UnauthorizedException("The employee account is inactive.");
            
            return employee;
        }

        private static void ValidateCanBeCancelled(Booking booking, bool allowAutomaticCompleted)
        {
            if (booking.Status == BookingStatus.Scheduled) return;
            
            if (booking.Status == BookingStatus.Completed)
            {
                if (allowAutomaticCompleted && booking.CompletionSource == CompletionSource.Automatic) return;

                if (booking.CompletionSource == CompletionSource.Manual) throw new ConflictException("A manually completed booking cannot be cancelled.");
                
                throw new ConflictException("The booking cannot be cancelled in its current state.");
            }

            if (booking.Status == BookingStatus.Cancelled) throw new ConflictException("The booking is already cancelled.");
            

            throw new ConflictException("The booking cannot be cancelled in its current state.");
        }

        private static void ValidateCanBeCancelledByEmployee(Booking booking)
        {
            if (booking.Status == BookingStatus.Cancelled)
            {
                throw new ConflictException("The booking is already cancelled.");
            }

            if (booking.Status != BookingStatus.Scheduled)
            {
                throw new ConflictException("Employees can only cancel scheduled bookings.");
            }

            if (booking.StartsAt <= DateTimeOffset.UtcNow)
            {
                throw new ConflictException("Employees can only cancel bookings that have not started yet.");
            }
        }

        public async Task CancelStaffBookingAndBlockAsync(
            long bookingId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long employeeId;
            if (currentUser.Role == UserRole.Employee)
            {
                Employee employee = await GetActiveEmployeeAsync(cancellationToken);
                employeeId = employee.Id;
            }
            else if (currentUser.Role == UserRole.Admin)
            {
                BookingReadResult? result = await bookingRepository.GetByIdAsync(
                    bookingId,
                    cancellationToken);

                if (result is null)
                {
                    throw new NotFoundException($"Booking with ID '{bookingId}' was not found.");
                }

                employeeId = result.Booking.EmployeeId;
            }
            else
            {
                throw new UnauthorizedException(
                    "Only employees and admins can cancel a staff booking and block its time.");
            }

            BookingReadResult? booking = await bookingRepository.GetByIdAsync(
                bookingId,
                cancellationToken);

            if (booking is null || booking.Booking.EmployeeId != employeeId)
            {
                throw new NotFoundException($"Booking with ID '{bookingId}' was not found.");
            }

            ValidateCanBeCancelledByEmployee(booking.Booking);

            bool cancelled = await bookingRepository.CancelAndBlockAsEmployeeAsync(
                bookingId,
                employeeId,
                $"Pauza nakon otkazanog termina #{bookingId}.",
                cancellationToken);

            if (!cancelled)
            {
                throw new ConflictException(
                    "The booking could not be cancelled because its state changed or it is no longer in the future.");
            }
        }

        private async Task CancelBookingInternalAsync(long bookingId, CancellationToken cancellationToken)
        {
            bool cancelled = await bookingRepository.CancelAsync(bookingId, cancellationToken);
            if (!cancelled) throw new ConflictException("The booking could not be cancelled because its status changed."); 
        }

        private async Task<Customer> GetActiveCustomerAsync(CancellationToken cancellationToken)
        {
            Customer? customer = await customerRepository.GetActiveByUserAccountIdAsync(currentUser.UserAccountId,cancellationToken);

            if (customer is null) throw new UnauthorizedException("The customer account is inactive or the customer profile was not found.");
            
            return customer;
        }

        private async Task<EmployeeBookingContext> GetBookableEmployeeAsync(long employeeId, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(employeeId,cancellationToken);
            if (employee is null) throw new NotFoundException($"Employee with ID '{employeeId}' was not found.");
            if (!employee.IsActive) throw new ConflictException($"Employee with ID '{employeeId}' is inactive.");
            
            UserAccount? userAccount =await userAccountRepository.GetByIdAsync(employee.UserAccountId, cancellationToken);

            if (userAccount is null) throw new NotFoundException($"User account with ID '{employee.UserAccountId}' was not found.");
            if (!userAccount.IsActive) throw new ConflictException($"Employee '{employeeId}' cannot receive bookings because the associated account is inactive.");

            return new EmployeeBookingContext(employee, userAccount);
        }

        private async Task<HoldSchedule> BuildHoldScheduleAsync(long employeeId, DateTimeOffset startsAt, IReadOnlyCollection<long> serviceIds, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<BookableService> services = await bookableServiceRepository.GetByIdsAsync(serviceIds, cancellationToken);

            IReadOnlyCollection<EmployeeServiceAssignemnt> assignments = await employeeServiceRepository.GetByEmployeeAndServiceIdsAsync(employeeId, serviceIds, cancellationToken);

            Dictionary<long, BookableService> servicesById = services.ToDictionary(service => service.Id);

            Dictionary<long, EmployeeServiceAssignemnt> assignmentsByServiceId = assignments.ToDictionary(assignment => assignment.ServiceId);

            List<BookingHoldService> holdServices = new(serviceIds.Count);

            DateTimeOffset currentStart = startsAt;

            int totalDurationMinutes = 0;
            decimal totalPrice = 0m;

            foreach (long serviceId in serviceIds)
            {
                if (!servicesById.TryGetValue(serviceId, out BookableService? service)) throw new NotFoundException($"Service with ID '{serviceId}' was not found.");
                if (!service.IsActive) throw new ConflictException($"Service with ID '{serviceId}' is inactive.");
                
                if (!assignmentsByServiceId.TryGetValue(serviceId, out EmployeeServiceAssignemnt? assignment)) throw new ConflictException($"Employee with ID '{employeeId}' does not provide service with ID '{serviceId}'.");
                if (!assignment.IsActive) throw new ConflictException($"Employee with ID '{employeeId}' does not currently provide service with ID '{serviceId}'.");
                

                DateTimeOffset appointmentEndsAt = currentStart.AddMinutes(service.DurationMinutes);

                holdServices.Add(
                    new BookingHoldService
                    {
                        ServiceId = service.Id,
                        Name = service.Name,
                        DurationMinutes = service.DurationMinutes,
                        Price = service.Price,
                        StartsAt = currentStart.ToUniversalTime(),
                        EndsAt = appointmentEndsAt.ToUniversalTime()
                    });

                currentStart = appointmentEndsAt;

                totalDurationMinutes +=service.DurationMinutes;

                totalPrice += service.Price;
            }

            return new HoldSchedule(
                holdServices,
                totalDurationMinutes,
                totalPrice,
                currentStart);
        }

        private static long[] ValidateServiceIds(IReadOnlyCollection<long> serviceIds)
        {
            if (serviceIds.Count == 0) throw new ValidationException("At least one service must be selected.");
            
            long[] distinctServiceIds = serviceIds.Distinct().ToArray();
            if (distinctServiceIds.Length != serviceIds.Count) throw new ConflictException("A service cannot be selected more than once.");

            return distinctServiceIds;
        }

        private static BookingResponse MapToResponse(Booking booking,IReadOnlyList<Appointment> appointments)
        {
            AppointmentResponse[] appointmentResponses = appointments.Select(MapAppointmentToResponse).ToArray();

            return new BookingResponse(
                booking.Id,
                booking.CustomerId ?? throw new InvalidOperationException(
                    "Customer bookings must have a customer ID."),
                booking.EmployeeId,
                booking.StartsAt,
                booking.EndsAt,
                booking.Status,
                booking.Notes,
                booking.CreatedAt,
                appointmentResponses);
        }

        private static AppointmentResponse MapAppointmentToResponse(Appointment appointment)
        {
            return new AppointmentResponse(
                 appointment.Id,
                 appointment.BookingId,
                 appointment.ServiceId,
                 appointment.ServiceNameAtBooking,
                 appointment.DurationMinutesAtBooking,
                 appointment.PriceAtBooking,
                 appointment.StartsAt,
                 appointment.EndsAt,
                 appointment.Status);
        }

        private async Task DeleteHoldSafelyAsync(BookingHold hold)
        {
            try
            {
                await bookingHoldRepository.DeleteAsync(hold, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to delete booking hold {HoldToken}.",
                    hold.HoldToken);
            }
        }

        private sealed record EmployeeBookingContext(
            Employee Employee,
            UserAccount UserAccount);
        private sealed record HoldSchedule(
            IReadOnlyList<BookingHoldService> Services,
            int TotalDurationMinutes,
            decimal TotalPrice,
            DateTimeOffset EndsAt);
    }

}
