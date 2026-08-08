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
using AppointmentBooking.Api.Services.Appointments;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Sdk;

namespace AppointmentBooking.Api.Tests.Services
{
    public class AppointmentServiceTests
    {
        private readonly Mock<IAppointmentRepository> appointmentRepository = new();

        private readonly Mock<IEmployeeRepository> employeeRepository = new();

        private readonly Mock<IBookableServiceRepository> bookableServiceRepository = new();

        private readonly Mock<IEmployeeServiceRepository> assignmentRepository = new();

        private readonly Mock<IOptions<AppointmentOptions>> options = new();

        private readonly Mock<ILogger<AppointmentService>> logger = new();

        private readonly TimeProvider timeProvider = TimeProvider.System;

        private readonly AppointmentService appointmentService;

        public AppointmentServiceTests()
        {
            options.Setup(x => x.Value).Returns(new AppointmentOptions
            {
                HoldDurationInMinutes = 5
            });

            appointmentService = new AppointmentService(
               appointmentRepository.Object,
               employeeRepository.Object,
               bookableServiceRepository.Object,
               assignmentRepository.Object,
               options.Object,
               timeProvider,
               logger.Object);
        }

        [Fact]
        public async Task CreateHoldAsync_ShouldCreateHold_WhenRequestIsValid()
        {
            var request = new CreateAppointmentHoldRequest(
                EmployeeId: 1,
                ServiceId: 1,
                StartsAt: DateTimeOffset.UtcNow.AddHours(1));

            employeeRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee
            {
                Id = 1,
                FirstName = "Test",
                LastName = "Employee",
                Email = "test@example.com",
                Phone = "1234567890",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });

            bookableServiceRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookableService
            {
                Id = 1,
                Name = "Test Service",
                Description = "Test Service Description",
                Price = 15m,
                IsActive = true,
                DurationMinutes = 30,
                CreatedAt = DateTimeOffset.UtcNow
            });

            assignmentRepository.
                Setup(x => x.GetAsync(1, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EmployeeServiceAssignemnt
                {
                    EmployeeId = 1,
                    ServiceId = 1,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                });

            Guid holdToken = Guid.NewGuid();

            DateTimeOffset startsAt = request.StartsAt;
            DateTimeOffset endsAt = startsAt.AddMinutes(30);
            DateTimeOffset holdExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

            Appointment appointment = new()
            {
                Id = 1,
                EmployeeId = 1,
                ServiceId = 1,
                StartsAt = startsAt,
                EndsAt = endsAt,
                Status = AppointmentStatus.Held,
                HoldToken = holdToken,
                HoldExpiresAt = holdExpiresAt,
                CreatedAt = DateTimeOffset.UtcNow
            };

            appointmentRepository
                .Setup(x => x.CreateHoldAsync(It.IsAny<CreateAppointmentHoldData>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(appointment);

            AppointmentHoldResponse result = await appointmentService.CreateHoldAsync(request, CancellationToken.None);

            result.Should().NotBeNull();

            result.id.Should().Be(appointment.Id);

            result.HoldToken.Should().Be(appointment.HoldToken!.Value);

            result.ExpiresAt.Should().Be(appointment.HoldExpiresAt!.Value);

            appointmentRepository.Verify(
                x => x.CreateHoldAsync(
                    It.IsAny<CreateAppointmentHoldData>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateHoldAsync_ShouldThrowConflictException_WhenAppointmentIsInThePast()
        {
            var request = new CreateAppointmentHoldRequest(
                EmployeeId: 1,
                ServiceId: 1,
                StartsAt: DateTimeOffset.UtcNow.AddHours(-1));

            Func<Task> act = async () => await appointmentService.CreateHoldAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>()
                .WithMessage("Appointments cannot be created in the past.");
        }

        [Fact]
        public async Task CreateHoldAsync_ShouldThrowNotFoundException_WhenEmployeeDoesNotExist()
        {
            var request = new CreateAppointmentHoldRequest(
                EmployeeId: 1,
                ServiceId: 1,
                StartsAt: DateTimeOffset.UtcNow.AddHours(1));

            employeeRepository
                .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Employee?)null);

            Func<Task> act = () => appointmentService.CreateHoldAsync(request, CancellationToken.None);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Employee with ID '1' was not found.");
        }

        [Fact]
        public async Task ConfirmAsync_ShouldThrowConflictException_WhenHoldExpired()
        {
            Guid holdToken = Guid.NewGuid();

            Appointment appointment = new()
            {
                Id = 1,
                EmployeeId = 1,
                ServiceId = 1,
                StartsAt = DateTimeOffset.UtcNow.AddHours(1),
                EndsAt = DateTimeOffset.UtcNow.AddHours(2),
                Status = AppointmentStatus.Held,
                HoldToken = holdToken,
                HoldExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                CreatedAt = DateTimeOffset.UtcNow
            };

            appointmentRepository
                .Setup(x => x.GetByHoldTokenAsync(
                    holdToken,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(appointment);

            var request = new ConfirmHoldAppointment(
                CustomerFirstName: "John",
                CustomerLastName: "Doe",
                CustomerEmail: "john@example.com",
                CustomerPhone: "061111111",
                Notes: null);

            Func<Task> act = () => appointmentService.ConfirmAsync(
                holdToken,
                request,
                CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>().WithMessage("The reservation hold has expired.");

            appointmentRepository.Verify(
                x => x.ConfirmHoldAsync(
                    It.IsAny<ConfirmAppointmentData>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }


        [Fact]
        public async Task GetAllAsync_ShouldReturnPagedAppointments()
        {
            var request = new GetAppointmentsRequest
            {
                EmployeeId = 1,
                ServiceId = 2,
                Status = AppointmentStatus.Scheduled,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Page = 2,
                PageSize = 10
            };

            var appointments = new[]
            {
                new AppointmentListItem
                {
                    Id = 1,
                    EmployeeId = 1,
                    EmployeeFirstName = "John",
                    EmployeeLastName = "Doe",
                    ServiceId = 2,
                    ServiceName = "Haircut",
                    CustomerFirstName = "Test",
                    CustomerLastName = "Customer",
                    CustomerEmail = "test@example.com",
                    CustomerPhone = "0611111111",
                    StartsAt = DateTimeOffset.UtcNow.AddHours(1),
                    EndsAt = DateTimeOffset.UtcNow.AddHours(2),
                    Status = AppointmentStatus.Scheduled,
                    Notes = null
                }
            };

            appointmentRepository
                .Setup(x => x.GetAllAsync(
                    It.IsAny<AppointmentSearchCriteria>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PagedResult<AppointmentListItem>
                 {
                    Items = appointments,
                    TotalCount = 21
                 });

            PagedResponse<AppointmentListItem> result = await appointmentService.GetAllAsync(request, CancellationToken.None);

            result.Should().NotBeNull();

            result.Items.Should().HaveCount(1);

            result.TotalCount.Should().Be(21);

            result.Page.Should().Be(2);

            result.PageSize.Should().Be(10);

            result.Items.First().Id.Should().Be(1);

            result.Items.First().EmployeeId.Should().Be(1);

            result.Items.First().ServiceId.Should().Be(2);

            appointmentRepository.Verify(
                x => x.GetAllAsync(
                    It.Is<AppointmentSearchCriteria>(
                        criteria => 
                        criteria.EmployeeId == 1 && 
                        criteria.ServiceId == 2 &&
                        criteria.Status  == AppointmentStatus.Scheduled && 
                        criteria.Date == request.Date &  
                        criteria.Page == 2 &&
                        criteria.PageSize == 10),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
