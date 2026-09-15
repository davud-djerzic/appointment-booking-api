using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.DTOs.Employees.Request;
using AppointmentBooking.Api.DTOs.Employees.Response;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Models.Enums;
using AppointmentBooking.Api.Repositories.Employees;
using AppointmentBooking.Api.Repositories.UserAccounts;
using AppointmentBooking.Api.Services.CurrentUser;
using Microsoft.AspNetCore.Identity;

namespace AppointmentBooking.Api.Services.Employees
{
    public sealed class EmployeeService(IEmployeeRepository employeeRepository, IUserAccountRepository userAccountRepository, IPasswordHasher<UserAccount> passwordHasher, ICurrentUserService currentUser) : IEmployeeService
    {
        public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken)
        {
            string email = request.Email.Trim();

            UserAccount? existingUserAccount = await userAccountRepository.GetByEmailAsync(email, cancellationToken); 
            if (existingUserAccount is not null) throw new ConflictException ("An account with this email already exists.");

            UserAccount userAccount = new()
            {
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                PasswordHash = string.Empty,
                Role = UserRole.Employee,
                IsActive = true
            };

            userAccount.PasswordHash =
               passwordHasher.HashPassword(
                   userAccount,
                   request.Password);

            Employee createdEmployee = await employeeRepository.CreateEmployeeAccountAsync(
                    userAccount,
                    request.Phone.Trim(),
                    cancellationToken);

            return MapToResponse(createdEmployee, userAccount);
        }

        public async Task<EmployeeResponse> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(id, cancellationToken);
            if (employee is null) throw new NotFoundException($"Employee with ID '{id}' was not found.");

            UserAccount? userAccount = await userAccountRepository.GetByIdAsync(employee.UserAccountId, cancellationToken);
            if (userAccount is null) throw new NotFoundException($"User account with ID '{employee.UserAccountId}' was not found.");

            return MapToResponse(employee, userAccount);
        }

        public async Task DeactivateAsync(long id, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(id, cancellationToken);
            if (employee is null) throw new NotFoundException($"Employee with ID '{id}' was not found.");
            if (!employee.IsActive) throw new ConflictException($"Employee with ID '{id}' is already inactive.");

            await employeeRepository.DeactivateAsync(id, cancellationToken);
        }

        public async Task<PagedResponse<EmployeeResponse>> GetAllAsync(GetEmployeesQuery query, CancellationToken cancellationToken)
        {
            PagedResult<EmployeeListItem> result = await employeeRepository.GetAllAsync(
              query.Search,
              query.IsActive,
              query.Page,
              query.PageSize,
              cancellationToken);

            EmployeeResponse[] employees =
                result.Items
                    .Select(MapToResponse)
                    .ToArray();

            return new PagedResponse<EmployeeResponse>
            {
                Items = employees,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = result.TotalCount
            };
        }

        public async Task<EmployeeResponse> UpdateAsync(long id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
        {
            Employee? existingEmployee = await employeeRepository.GetByIdAsync(id, cancellationToken);
            if (existingEmployee is not null) throw new NotFoundException($"Employee with ID '{id}' was not found.");

            string email = request.Email.Trim();

            UserAccount? existingUserAccount = await userAccountRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUserAccount is not null && existingUserAccount.Id != existingEmployee.UserAccountId) throw new ConflictException("An account with this email already exists.");

            UpdateEmployeeData data = new()
            {
                Id = id,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Email = email,
                Phone = request.Phone.Trim()
            };

            Employee updatedEmployee = await employeeRepository.UpdateAsync(data, cancellationToken);

            UserAccount? updatedUserAccount = await userAccountRepository.GetByIdAsync(updatedEmployee.UserAccountId,cancellationToken);

            if (updatedUserAccount is null) throw new NotFoundException( $"User account with ID '{updatedEmployee.UserAccountId}' was not found.");

            return MapToResponse(updatedEmployee, updatedUserAccount);
        }

        public async Task<EmployeeResponse> ActivateAsync(long id, CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByIdAsync(id, cancellationToken);
            if (employee is null) throw new NotFoundException($"Employee with ID '{id}' was not found.");
            if (employee.IsActive) throw new ConflictException($"Employee with ID '{id}' is already active.");

            Employee activatedEmployee = await employeeRepository.ActivateAsync(id, cancellationToken);

            UserAccount? userAccount = await userAccountRepository.GetByIdAsync(activatedEmployee.UserAccountId, cancellationToken);

            if (userAccount is null) throw new NotFoundException($"User account with ID '{activatedEmployee.UserAccountId}' was not found.");
            

            return MapToResponse(activatedEmployee, userAccount);
        }

        public async Task<EmployeeResponse> GetMyProfileAsync(CancellationToken cancellationToken)
        {
            Employee employee = await GetCurrentEmployeeAsync(cancellationToken);

            UserAccount? userAccount = await userAccountRepository.GetByIdAsync(employee.UserAccountId, cancellationToken);
            if (userAccount is null) throw new NotFoundException("User account was not found");

            return MapToResponse(employee, userAccount);
        }


        private static EmployeeResponse MapToResponse(Employee employee, UserAccount userAccount) 
        {
            return new EmployeeResponse(
                employee.Id,
                userAccount.FirstName,
                userAccount.LastName,
                userAccount.Email,
                employee.Phone,
                employee.IsActive,
                employee.CreatedAt,
                employee.UpdatedAt);
        }

        private static EmployeeResponse MapToResponse(EmployeeListItem employee)
        {
            return new EmployeeResponse(
                employee.Id,
                employee.FirstName,
                employee.LastName,
                employee.Email,
                employee.Phone,
                employee.IsActive,
                employee.CreatedAt,
                employee.UpdatedAt);
        }

        private async Task<Employee> GetCurrentEmployeeAsync(CancellationToken cancellationToken)
        {
            Employee? employee = await employeeRepository.GetByUserAccountIdAsync(currentUser.UserAccountId, cancellationToken);

            if (employee is null) throw new NotFoundException("Employee profile was not found.");
            
            return employee;
        }
    }
}
