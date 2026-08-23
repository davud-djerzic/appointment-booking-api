using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Request;
using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Response;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.Employees;
using AppointmentBooking.Api.Repositories.EmployeeWorkingHoursRepository;
using Dapper;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;

namespace AppointmentBooking.Api.Services.EmployeeWorkingHoursService
{
    public class EmployeeWorkingHoursService(IEmployeeWorkingHoursRepository employeeWorkingHoursRepository, IEmployeeRepository employeeRepository) : IEmployeeWorkingHoursService
    {
        public async Task<EmployeeWorkingHoursResponse> CreateAsync(long employeeId, CreateEmployeeWorkingHoursRequest request, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee == null) throw new NotFoundException($"Employee with ID '{employeeId}' was not found.");
            if (!employee.IsActive) throw new ConflictException($"Employee with ID '{employeeId}' is inactive.");

            if (!Enum.IsDefined(request.DayOfWeek)) throw new ValidationException("DayOfWeek must be a valid weekday.");
           

            EmployeeWorkingHours employeeWorkingHours = new()
            {
                EmployeeId = employeeId,
                DayOfWeek = request.DayOfWeek,
                StartsAt = request.StartsAt,
                EndsAt = request.EndsAt,
            };

            EmployeeWorkingHours created = await employeeWorkingHoursRepository.CreateAsync(employeeWorkingHours, cancellationToken);

            return new EmployeeWorkingHoursResponse(
                created.Id,
                created.EmployeeId,
                created.DayOfWeek,
                created.StartsAt,
                created.EndsAt);
        }

        public async Task<EmployeeWorkingHoursResponse> GetByIdAsync(long employeeId, long workingHoursId, CancellationToken cancellationToken)
        {
            EmployeeWorkingHours? employeeWorkingHours = await employeeWorkingHoursRepository.GetByIdAsync(employeeId, workingHoursId, cancellationToken);

            if (employeeWorkingHours is null) throw new NotFoundException($"Working hours with ID '{workingHoursId}' were not found for employee '{employeeId}'.");

            return new EmployeeWorkingHoursResponse(
                employeeWorkingHours.Id,
                employeeWorkingHours.EmployeeId,
                employeeWorkingHours.DayOfWeek,
                employeeWorkingHours.StartsAt,
                employeeWorkingHours.EndsAt);
        }

        public async Task<IReadOnlyCollection<EmployeeWorkingHoursResponse>> GetByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);

            if (employee is null) throw new NotFoundException($"Employee with ID '{employeeId}' was not found.");
            if (!employee.IsActive) throw new ConflictException($"Employee with ID '{employeeId}' is inactive.");

            IReadOnlyCollection<EmployeeWorkingHours> workingHours = await employeeWorkingHoursRepository.GetByEmployeeIdAsync(employeeId, cancellationToken);

            return workingHours.Select(workingHour => new EmployeeWorkingHoursResponse(
                workingHour.Id,
                workingHour.EmployeeId,
                workingHour.DayOfWeek,
                workingHour.StartsAt,
                workingHour.EndsAt))
            .ToArray();
        }

        public async Task<EmployeeWorkingHoursResponse> UpdateAsync(long employeeId, long workingHoursId, UpdateEmployeeWorkingHoursRequest request, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null) throw new NotFoundException($"Employee with ID '{employeeId}' was not found.");
            if (!employee.IsActive) throw new ConflictException($"Employee with ID '{employeeId}' is inactive.");

            EmployeeWorkingHours? employeeWorkingHours = await employeeWorkingHoursRepository.GetByIdAsync(employeeId, workingHoursId, cancellationToken);
            if (employeeWorkingHours is null) throw new NotFoundException($"Working hours with ID '{workingHoursId}' were not found for employee '{employeeId}'.");

            EmployeeWorkingHours workingHours = new()
            {
                Id = workingHoursId,
                EmployeeId = employeeId,
                DayOfWeek = request.DayOfWeek,
                StartsAt = request.StartsAt,
                EndsAt = request.EndsAt
            };

            EmployeeWorkingHours? updated =
                await employeeWorkingHoursRepository.UpdateAsync(
                    employeeId,
                    workingHoursId,
                    workingHours,
                    cancellationToken);

            return new EmployeeWorkingHoursResponse(
               updated.Id,
               updated.EmployeeId,
               updated.DayOfWeek,
               updated.StartsAt,
               updated.EndsAt);
        }

        public async Task DeleteAsync(long employeeId, long workingHoursId, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);

            if (employee is null)throw new NotFoundException($"Employee with ID '{employeeId}' was not found.");
            

            if (!employee.IsActive) throw new ConflictException($"Employee with ID '{employeeId}' is inactive.");
            

            EmployeeWorkingHours? employeeWorkingHours = await employeeWorkingHoursRepository.GetByIdAsync(employeeId, workingHoursId, cancellationToken);

            if (employeeWorkingHours is null) throw new NotFoundException($"Working hours with ID '{workingHoursId}' were not found for employee '{employeeId}'.");
            

            await employeeWorkingHoursRepository.DeleteAsync(employeeId, workingHoursId, cancellationToken);
        }
    }
}
