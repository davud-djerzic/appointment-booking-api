using AppointmentBooking.Api.DTOs.EmployeeServices.Responses;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.Employees;
using AppointmentBooking.Api.Repositories.EmployeeServices;
using AppointmentBooking.Api.Repositories.Services;

namespace AppointmentBooking.Api.Services.EmployeeServices
{
    public sealed class EmployeeServiceAssignmentService(IEmployeeRepository employeeRepository, IBookableServiceRepository serviceRepository, IEmployeeServiceRepository assignmentRepository) : IEmployeeServiceAssignmentService
    {
        public async Task<AssignEmployeeServiceResult> AssignAsync(long employeeId, long serviceId, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null) return new AssignEmployeeServiceResult(AssignEmployeeServiceStatus.EmployeeNotFound);
            if (!employee.IsActive) return new AssignEmployeeServiceResult(AssignEmployeeServiceStatus.Conflict, Message: "Employee is inactive.");

            BookableService? service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service is null) return new AssignEmployeeServiceResult(AssignEmployeeServiceStatus.ServiceNotFound);
            if (!service.IsActive) return new AssignEmployeeServiceResult(AssignEmployeeServiceStatus.Conflict, Message: "Service is inactive.");

            var existingAssignment = await assignmentRepository.GetAsync(employeeId, serviceId, cancellationToken);
            if (existingAssignment?.IsActive == true) return new AssignEmployeeServiceResult(AssignEmployeeServiceStatus.Conflict, Message: "Employee already provides this service.");

            EmployeeServiceAssignemnt assignment = await assignmentRepository.AssignOrReactivateAsync(employeeId, serviceId, cancellationToken);

            AssignEmployeeServiceStatus status = existingAssignment is null ? AssignEmployeeServiceStatus.Assigned : AssignEmployeeServiceStatus.Reactivated;

            EmployeeServiceAssignmentResponse response = new EmployeeServiceAssignmentResponse(assignment.EmployeeId, assignment.ServiceId, assignment.IsActive, assignment.CreatedAt, assignment.UpdatedAt);

            return new AssignEmployeeServiceResult(status, response);
        }

        public async Task<DeactivateEmployeeServiceResult> DeactivateAsync(long employeeId, long serviceId, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null) return new(DeactivateEmployeeServiceStatus.EmployeeNotFound);

            BookableService? service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service is null) return new(DeactivateEmployeeServiceStatus.ServiceNotFound);

            EmployeeServiceAssignemnt? assignment = await assignmentRepository.GetAsync(employeeId, serviceId, cancellationToken);
            if (assignment is null || !assignment.IsActive) return new(DeactivateEmployeeServiceStatus.AssignmentNotFound);

            await assignmentRepository.DeactivateAsync(employeeId, serviceId, cancellationToken);

            return new(DeactivateEmployeeServiceStatus.Success);
        }

        public async Task<IEnumerable<EmployeeServiceResponse>> GetEmployeeServicesAsync(long employeeId, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null) throw new NotFoundException($"Employee with ID '{employeeId}' was not found.");

            return await assignmentRepository.GetEmployeeServicesAsync(employeeId, cancellationToken);
        }

        public async Task<IEnumerable<ServiceEmployeeResponse>> GetServiceEmployeesAsync(long serviceId, CancellationToken cancellationToken)
        {
            BookableService? service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service is null) throw new NotFoundException($"Service with ID '{serviceId}' was not found.");

            return await assignmentRepository.GetServiceEmployeesAsync(serviceId, cancellationToken);
        }

        public async Task<EmployeeServiceAssignmentResponse> GetServiceEmployeesAssignmentsAsync(long employeeId, long serviceId, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null) throw new NotFoundException($"Employee with ID '{employeeId}' was not found.");

            BookableService? service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service is null) throw new NotFoundException($"Service with ID '{serviceId}' was not found.");

            EmployeeServiceAssignemnt assignment = await assignmentRepository.GetServiceEmployeesAssignmentAsync(employeeId, serviceId, cancellationToken);

            return new EmployeeServiceAssignmentResponse(assignment.EmployeeId, assignment.ServiceId, assignment.IsActive, assignment.CreatedAt, assignment.UpdatedAt);
        }
    }
}
