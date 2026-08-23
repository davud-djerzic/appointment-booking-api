using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Request;
using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Response;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.Employees;
using AppointmentBooking.Api.Repositories.EmployeeWorkingHoursRepository;
using AppointmentBooking.Api.Services.EmployeeWorkingHoursService;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace AppointmentBooking.Api.Tests.Services
{
    public sealed class EmployeeWorkingHoursServiceTests
    {
        private readonly Mock<IEmployeeRepository> employeeRepository = new();
        private readonly Mock<IEmployeeWorkingHoursRepository> employeeWorkingHoursRepository = new();

        private readonly EmployeeWorkingHoursService service;

        public EmployeeWorkingHoursServiceTests()
        {
            service = new EmployeeWorkingHoursService(
                employeeWorkingHoursRepository.Object,
                employeeRepository.Object);
        }

        private static Employee CreateActiveEmployee(long id = 1)
        {
            return new Employee
            {
                Id = id,
                FirstName = "Test",
                LastName = "Employee",
                Email = "test@example.com",
                Phone = "123456789",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        private static Employee CreateInactiveEmployee(long id = 1)
        {
            return new Employee
            {
                Id = id,
                FirstName = "Test",
                LastName = "Employee",
                Email = "test@example.com",
                Phone = "123456789",
                IsActive = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        private static EmployeeWorkingHours CreateWorkingHours(
            long id = 1,
            long employeeId = 1,
            WeekDay dayOfWeek = WeekDay.Monday,
            TimeOnly? startsAt = null,
            TimeOnly? endsAt = null)
        {
            return new EmployeeWorkingHours
            {
                Id = id,
                EmployeeId = employeeId,
                DayOfWeek = dayOfWeek,
                StartsAt = startsAt ?? new TimeOnly(9, 0),
                EndsAt = endsAt ?? new TimeOnly(17, 0)
            };
        }

        private static CreateEmployeeWorkingHoursRequest CreateCreateRequest(
            WeekDay dayOfWeek = WeekDay.Monday,
            int startHour = 9,
            int endHour = 17)
        {
            return new CreateEmployeeWorkingHoursRequest
            {
                DayOfWeek = dayOfWeek,
                StartsAt = new TimeOnly(startHour, 0),
                EndsAt = new TimeOnly(endHour, 0)
            };
        }

        private static UpdateEmployeeWorkingHoursRequest CreateUpdateRequest(
            WeekDay dayOfWeek = WeekDay.Tuesday,
            int startHour = 10,
            int endHour = 18)
        {
            return new UpdateEmployeeWorkingHoursRequest
            {
                DayOfWeek = dayOfWeek,
                StartsAt = new TimeOnly(startHour, 0),
                EndsAt = new TimeOnly(endHour, 0)
            };
        }

        private void SetupEmployeeNotFound(long employeeId = 1)
        {
            employeeRepository
                .Setup(x => x.GetByIdAsync(
                    employeeId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Employee?)null);
        }

        private void SetupActiveEmployee(long employeeId = 1)
        {
            employeeRepository
                .Setup(x => x.GetByIdAsync(
                    employeeId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateActiveEmployee(employeeId));
        }

        private void SetupInactiveEmployee(long employeeId = 1)
        {
            employeeRepository
                .Setup(x => x.GetByIdAsync(
                    employeeId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateInactiveEmployee(employeeId));
        }

        // ------------------------------------------------------------
        // CREATE
        // ------------------------------------------------------------

        [Fact]
        public async Task CreateAsync_ShouldThrowNotFoundException_WhenEmployeeDoesNotExist()
        {
            const long employeeId = 1;

            SetupEmployeeNotFound(employeeId);

            Func<Task> act = () =>
                service.CreateAsync(
                    employeeId,
                    CreateCreateRequest(),
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>()
                .WithMessage("Employee with ID '1' was not found.");

            employeeWorkingHoursRepository.Verify(
                x => x.CreateAsync(
                    It.IsAny<EmployeeWorkingHours>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldThrowConflictException_WhenEmployeeIsInactive()
        {
            const long employeeId = 1;

            SetupInactiveEmployee(employeeId);

            Func<Task> act = () =>
                service.CreateAsync(
                    employeeId,
                    CreateCreateRequest(),
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<ConflictException>()
                .WithMessage("Employee with ID '1' is inactive.");

            employeeWorkingHoursRepository.Verify(
                x => x.CreateAsync(
                    It.IsAny<EmployeeWorkingHours>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ShouldReturnCreatedWorkingHours_WhenRequestIsValid()
        {
            const long employeeId = 1;

            SetupActiveEmployee(employeeId);

            CreateEmployeeWorkingHoursRequest request =
                CreateCreateRequest(
                    WeekDay.Monday,
                    9,
                    17);

            EmployeeWorkingHours createdWorkingHours =
                CreateWorkingHours(
                    id: 10,
                    employeeId: employeeId,
                    dayOfWeek: WeekDay.Monday,
                    startsAt: new TimeOnly(9, 0),
                    endsAt: new TimeOnly(17, 0));

            employeeWorkingHoursRepository
                .Setup(x => x.CreateAsync(
                    It.IsAny<EmployeeWorkingHours>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdWorkingHours);

            EmployeeWorkingHoursResponse result =
                await service.CreateAsync(
                    employeeId,
                    request,
                    CancellationToken.None);

            result.Should().BeEquivalentTo(
                new EmployeeWorkingHoursResponse(
                    10,
                    employeeId,
                    WeekDay.Monday,
                    new TimeOnly(9, 0),
                    new TimeOnly(17, 0)));

            employeeWorkingHoursRepository.Verify(
                x => x.CreateAsync(
                    It.Is<EmployeeWorkingHours>(workingHours =>
                        workingHours.EmployeeId == employeeId &&
                        workingHours.DayOfWeek == WeekDay.Monday &&
                        workingHours.StartsAt == new TimeOnly(9, 0) &&
                        workingHours.EndsAt == new TimeOnly(17, 0)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ShouldPropagateOverlapException_WhenWorkingHoursOverlap()
        {
            const long employeeId = 1;

            SetupActiveEmployee(employeeId);

            employeeWorkingHoursRepository
                .Setup(x => x.CreateAsync(
                    It.IsAny<EmployeeWorkingHours>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ConflictException(
                        "Employee already has overlapping working hours."));

            Func<Task> act = () =>
                service.CreateAsync(
                    employeeId,
                    CreateCreateRequest(),
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<ConflictException>()
                .WithMessage(
                    "Employee already has overlapping working hours.");
        }

        // ------------------------------------------------------------
        // GET BY ID
        // ------------------------------------------------------------

        [Fact]
        public async Task GetByIdAsync_ShouldThrowNotFoundException_WhenWorkingHoursDoNotExist()
        {
            const long employeeId = 1;
            const long workingHoursId = 10;

            SetupActiveEmployee(employeeId);

            employeeWorkingHoursRepository
                .Setup(x => x.GetByIdAsync(
                    employeeId,
                    workingHoursId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmployeeWorkingHours?)null);

            Func<Task> act = () =>
                service.GetByIdAsync(
                    employeeId,
                    workingHoursId,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>()
                .WithMessage(
                    "Working hours with ID '10' were not found for employee '1'.");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnWorkingHours_WhenWorkingHoursExist()
        {
            const long employeeId = 1;
            const long workingHoursId = 10;

            SetupActiveEmployee(employeeId);

            EmployeeWorkingHours workingHours =
                CreateWorkingHours(
                    workingHoursId,
                    employeeId,
                    WeekDay.Monday,
                    new TimeOnly(9, 0),
                    new TimeOnly(17, 0));

            employeeWorkingHoursRepository
                .Setup(x => x.GetByIdAsync(
                    employeeId,
                    workingHoursId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(workingHours);

            EmployeeWorkingHoursResponse result =
                await service.GetByIdAsync(
                    employeeId,
                    workingHoursId,
                    CancellationToken.None);

            result.Should().BeEquivalentTo(
                new EmployeeWorkingHoursResponse(
                    workingHoursId,
                    employeeId,
                    WeekDay.Monday,
                    new TimeOnly(9, 0),
                    new TimeOnly(17, 0)));
        }

        // ------------------------------------------------------------
        // GET BY EMPLOYEE ID
        // ------------------------------------------------------------

        [Fact]
        public async Task GetByEmployeeIdAsync_ShouldThrowNotFoundException_WhenEmployeeDoesNotExist()
        {
            const long employeeId = 1;

            SetupEmployeeNotFound(employeeId);

            Func<Task> act = () =>
                service.GetByEmployeeIdAsync(
                    employeeId,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>()
                .WithMessage("Employee with ID '1' was not found.");

            employeeWorkingHoursRepository.Verify(
                x => x.GetByEmployeeIdAsync(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByEmployeeIdAsync_ShouldThrowConflictException_WhenEmployeeIsInactive()
        {
            const long employeeId = 1;

            SetupInactiveEmployee(employeeId);

            Func<Task> act = () =>
                service.GetByEmployeeIdAsync(
                    employeeId,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<ConflictException>()
                .WithMessage("Employee with ID '1' is inactive.");

            employeeWorkingHoursRepository.Verify(
                x => x.GetByEmployeeIdAsync(
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetByEmployeeIdAsync_ShouldReturnEmptyCollection_WhenEmployeeHasNoWorkingHours()
        {
            const long employeeId = 1;

            SetupActiveEmployee(employeeId);

            employeeWorkingHoursRepository
                .Setup(x => x.GetByEmployeeIdAsync(
                    employeeId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<EmployeeWorkingHours>());

            IReadOnlyCollection<EmployeeWorkingHoursResponse> result =
                await service.GetByEmployeeIdAsync(
                    employeeId,
                    CancellationToken.None);

            result.Should().BeEmpty();

            employeeWorkingHoursRepository.Verify(
                x => x.GetByEmployeeIdAsync(
                    employeeId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetByEmployeeIdAsync_ShouldReturnWorkingHours_WhenEmployeeHasWorkingHours()
        {
            const long employeeId = 1;

            SetupActiveEmployee(employeeId);

            EmployeeWorkingHours first =
                CreateWorkingHours(
                    id: 1,
                    employeeId: employeeId,
                    dayOfWeek: WeekDay.Monday,
                    startsAt: new TimeOnly(9, 0),
                    endsAt: new TimeOnly(12, 0));

            EmployeeWorkingHours second =
                CreateWorkingHours(
                    id: 2,
                    employeeId: employeeId,
                    dayOfWeek: WeekDay.Monday,
                    startsAt: new TimeOnly(13, 0),
                    endsAt: new TimeOnly(17, 0));

            employeeWorkingHoursRepository
                .Setup(x => x.GetByEmployeeIdAsync(
                    employeeId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { first, second });

            IReadOnlyCollection<EmployeeWorkingHoursResponse> result =
                await service.GetByEmployeeIdAsync(
                    employeeId,
                    CancellationToken.None);

            result.Should().HaveCount(2);

            result.Should().ContainSingle(x =>
                x.Id == 1 &&
                x.EmployeeId == employeeId &&
                x.DayOfWeek == WeekDay.Monday &&
                x.StartsAt == new TimeOnly(9, 0) &&
                x.EndsAt == new TimeOnly(12, 0));

            result.Should().ContainSingle(x =>
                x.Id == 2 &&
                x.EmployeeId == employeeId &&
                x.DayOfWeek == WeekDay.Monday &&
                x.StartsAt == new TimeOnly(13, 0) &&
                x.EndsAt == new TimeOnly(17, 0));
        }

        // ------------------------------------------------------------
        // UPDATE
        // ------------------------------------------------------------

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenEmployeeDoesNotExist()
        {
            const long employeeId = 1;
            const long workingHoursId = 10;

            SetupEmployeeNotFound(employeeId);

            Func<Task> act = () =>
                service.UpdateAsync(
                    employeeId,
                    workingHoursId,
                    CreateUpdateRequest(),
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>()
                .WithMessage("Employee with ID '1' was not found.");

            employeeWorkingHoursRepository.Verify(
                x => x.GetByIdAsync(
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            employeeWorkingHoursRepository.Verify(
                x => x.UpdateAsync(
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<EmployeeWorkingHours>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowConflictException_WhenEmployeeIsInactive()
        {
            const long employeeId = 1;
            const long workingHoursId = 10;

            SetupInactiveEmployee(employeeId);

            Func<Task> act = () =>
                service.UpdateAsync(
                    employeeId,
                    workingHoursId,
                    CreateUpdateRequest(),
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<ConflictException>()
                .WithMessage("Employee with ID '1' is inactive.");

            employeeWorkingHoursRepository.Verify(
                x => x.GetByIdAsync(
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            employeeWorkingHoursRepository.Verify(
                x => x.UpdateAsync(
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<EmployeeWorkingHours>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldThrowNotFoundException_WhenWorkingHoursDoNotExist()
        {
            const long employeeId = 1;
            const long workingHoursId = 10;

            SetupActiveEmployee(employeeId);

            employeeWorkingHoursRepository
                .Setup(x => x.GetByIdAsync(
                    employeeId,
                    workingHoursId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmployeeWorkingHours?)null);

            Func<Task> act = () =>
                service.UpdateAsync(
                    employeeId,
                    workingHoursId,
                    CreateUpdateRequest(),
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>()
                .WithMessage(
                    "Working hours with ID '10' were not found for employee '1'.");

            employeeWorkingHoursRepository.Verify(
                x => x.UpdateAsync(
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<EmployeeWorkingHours>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnUpdatedWorkingHours_WhenRequestIsValid()
        {
            const long employeeId = 1;
            const long workingHoursId = 10;

            SetupActiveEmployee(employeeId);

            EmployeeWorkingHours existingWorkingHours =
                CreateWorkingHours(
                    workingHoursId,
                    employeeId,
                    WeekDay.Monday,
                    new TimeOnly(9, 0),
                    new TimeOnly(17, 0));

            EmployeeWorkingHours updatedWorkingHours =
                CreateWorkingHours(
                    workingHoursId,
                    employeeId,
                    WeekDay.Tuesday,
                    new TimeOnly(10, 0),
                    new TimeOnly(18, 0));

            employeeWorkingHoursRepository
                .Setup(x => x.GetByIdAsync(
                    employeeId,
                    workingHoursId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingWorkingHours);

            employeeWorkingHoursRepository
                .Setup(x => x.UpdateAsync(
                    employeeId,
                    workingHoursId,
                    It.IsAny<EmployeeWorkingHours>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(updatedWorkingHours);

            EmployeeWorkingHoursResponse result =
                await service.UpdateAsync(
                    employeeId,
                    workingHoursId,
                    CreateUpdateRequest(
                        WeekDay.Tuesday,
                        10,
                        18),
                    CancellationToken.None);

            result.Should().BeEquivalentTo(
                new EmployeeWorkingHoursResponse(
                    workingHoursId,
                    employeeId,
                    WeekDay.Tuesday,
                    new TimeOnly(10, 0),
                    new TimeOnly(18, 0)));

            employeeWorkingHoursRepository.Verify(
                x => x.UpdateAsync(
                    employeeId,
                    workingHoursId,
                    It.Is<EmployeeWorkingHours>(workingHours =>
                        workingHours.EmployeeId == employeeId &&
                        workingHours.DayOfWeek == WeekDay.Tuesday &&
                        workingHours.StartsAt == new TimeOnly(10, 0) &&
                        workingHours.EndsAt == new TimeOnly(18, 0)),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldPropagateOverlapException_WhenUpdatedHoursOverlap()
        {
            const long employeeId = 1;
            const long workingHoursId = 10;

            SetupActiveEmployee(employeeId);

            employeeWorkingHoursRepository
                .Setup(x => x.GetByIdAsync(
                    employeeId,
                    workingHoursId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    CreateWorkingHours(
                        workingHoursId,
                        employeeId));

            employeeWorkingHoursRepository
                .Setup(x => x.UpdateAsync(
                    employeeId,
                    workingHoursId,
                    It.IsAny<EmployeeWorkingHours>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(
                    new ConflictException(
                        "Employee already has overlapping working hours."));

            Func<Task> act = () =>
                service.UpdateAsync(
                    employeeId,
                    workingHoursId,
                    CreateUpdateRequest(),
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<ConflictException>()
                .WithMessage(
                    "Employee already has overlapping working hours.");
        }

        // ------------------------------------------------------------
        // DELETE
        // ------------------------------------------------------------

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenEmployeeDoesNotExist()
        {
            const long employeeId = 1;
            const long workingHoursId = 10;

            SetupEmployeeNotFound(employeeId);

            Func<Task> act = () =>
                service.DeleteAsync(
                    employeeId,
                    workingHoursId,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>()
                .WithMessage("Employee with ID '1' was not found.");

            employeeWorkingHoursRepository.Verify(
                x => x.GetByIdAsync(
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            employeeWorkingHoursRepository.Verify(
                x => x.DeleteAsync(
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowConflictException_WhenEmployeeIsInactive()
        {
            const long employeeId = 1;
            const long workingHoursId = 10;

            SetupInactiveEmployee(employeeId);

            Func<Task> act = () =>
                service.DeleteAsync(
                    employeeId,
                    workingHoursId,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<ConflictException>()
                .WithMessage("Employee with ID '1' is inactive.");

            employeeWorkingHoursRepository.Verify(
                x => x.GetByIdAsync(
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            employeeWorkingHoursRepository.Verify(
                x => x.DeleteAsync(
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowNotFoundException_WhenWorkingHoursDoNotExist()
        {
            const long employeeId = 1;
            const long workingHoursId = 10;

            SetupActiveEmployee(employeeId);

            employeeWorkingHoursRepository
                .Setup(x => x.GetByIdAsync(
                    employeeId,
                    workingHoursId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmployeeWorkingHours?)null);

            Func<Task> act = () =>
                service.DeleteAsync(
                    employeeId,
                    workingHoursId,
                    CancellationToken.None);

            await act.Should()
                .ThrowAsync<NotFoundException>()
                .WithMessage(
                    "Working hours with ID '10' were not found for employee '1'.");

            employeeWorkingHoursRepository.Verify(
                x => x.DeleteAsync(
                    It.IsAny<long>(),
                    It.IsAny<long>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ShouldDeleteWorkingHours_WhenWorkingHoursExist()
        {
            const long employeeId = 1;
            const long workingHoursId = 10;

            SetupActiveEmployee(employeeId);

            employeeWorkingHoursRepository
                .Setup(x => x.GetByIdAsync(
                    employeeId,
                    workingHoursId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                    CreateWorkingHours(
                        workingHoursId,
                        employeeId));

            employeeWorkingHoursRepository
                .Setup(x => x.DeleteAsync(
                    employeeId,
                    workingHoursId,
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Func<Task> act = () =>
                service.DeleteAsync(
                    employeeId,
                    workingHoursId,
                    CancellationToken.None);

            await act.Should().NotThrowAsync();

            employeeWorkingHoursRepository.Verify(
                x => x.DeleteAsync(
                    employeeId,
                    workingHoursId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
