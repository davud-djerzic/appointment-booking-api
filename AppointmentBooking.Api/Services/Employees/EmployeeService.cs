using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.DTOs.Employees.Request;
using AppointmentBooking.Api.DTOs.Employees.Response;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.Employees;

namespace AppointmentBooking.Api.Services.Employees
{
    public sealed class EmployeeService(IEmployeeRepository employeeRepository) : IEmployeeService
    {
        public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken)
        {
            var employee = new Employee
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone.Trim()
            };
            
            var createdEmployee = await employeeRepository.CreateAsync(employee, cancellationToken);

            return MapToResponse(createdEmployee);
        }

        public async Task<EmployeeResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            var employee = await employeeRepository.GetByIdAsync(id, cancellationToken);
            
            return employee is null ? null : MapToResponse(employee);
        }

        public Task<bool> DeactivateAsync(long id, CancellationToken cancellationToken)
        {
            return employeeRepository.DeactivateAsync(id, cancellationToken);
        }

        public async Task<PagedResponse<EmployeeResponse>> GetAllAsync(GetEmployeesQuery query, CancellationToken cancellationToken)
        {
            var result = await employeeRepository.GetAllAsync(query.Search, query.IsActive, query.Page, query.PageSize, cancellationToken);

            var employees = result.Items.Select(MapToResponse).ToArray();

            return new PagedResponse<EmployeeResponse>
            {
                Items = employees,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = result.TotalCount
            };
        }

        public async Task<EmployeeResponse?> UpdateAsync(long id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
        {
            UpdateEmployeeData employees = new UpdateEmployeeData
            {
                Id = id,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone.Trim(),
            };

            var updatedEmployee = await employeeRepository.UpdateAsync(employees, cancellationToken);

            return updatedEmployee is null ? null : MapToResponse(updatedEmployee);
        }

        public async Task<EmployeeResponse?> ActivateAsync(long id, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.ActivateAsync(id, cancellationToken);

            return employee is null ? null : MapToResponse(employee);
        }


        private static EmployeeResponse MapToResponse(Employee employee)
        {
            return new EmployeeResponse
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                Email = employee.Email,
                Phone = employee.Phone,
                IsActive = employee.IsActive,
                CreatedAt = employee.CreatedAt,
                UpdatedAt = employee.UpdatedAt
            };
        }
    }
}
