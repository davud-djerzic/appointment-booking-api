using AppointmentBooking.Api.Models;
using Dapper;
using Npgsql;
using AppointmentBooking.Api.Exceptions;

namespace AppointmentBooking.Api.Repositories.Employees
{
    public sealed class EmployeeRepository(NpgsqlDataSource dataSource) : IEmployeeRepository
    {
        public async Task<Employee> CreateAsync(Employee employee, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO employees (first_name, last_name, email, phone)
                VALUES (@FirstName, @LastName, @Email, @Phone)
                RETURNING 
                    id,
                    first_name AS FirstName,
                    last_name AS LastName,
                    email AS Email,
                    phone AS Phone,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

                var command = new CommandDefinition(sql, employee, cancellationToken: cancellationToken);

                return await connection.QuerySingleAsync<Employee>(command);
            }
            catch (PostgresException exception)
                when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new EmployeeEmailAlreadyExistsException(employee.Email, exception);
            }
        }

        public async Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            const string sql = """
                    SELECT
                        id,
                        first_name AS FirstName,
                        last_name AS LastName,
                        email AS Email,
                        phone AS Phone,
                        is_active AS IsActive,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM employees
                    WHERE id = @Id;
                    """;
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

            var command = new CommandDefinition(
                sql,
                new { Id = id },
                cancellationToken : cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<Employee>(command);
        }

        public async Task DeactivateAsync(long employeeId, CancellationToken cancellationToken)
        {
            const string deactivateEmployeeSql = """
                    UPDATE employees 
                    SET 
                        updated_at =
                        CASE 
                            WHEN is_active = TRUE THEN NOW()
                            ELSE updated_at
                        END,
                        is_active = FALSE
                    WHERE id = @EmployeeId; 
                    """;

            const string deactivateAssignmentsSql = """
                UPDATE employee_services 
                SET 
                    updated_at =
                    CASE 
                        WHEN is_active = TRUE THEN NOW()
                        ELSE updated_at
                    END,
                    is_active = FALSE
                WHERE employee_id = @EmployeeId;
                """;

            const string deleteWorkingHoursSql = """
                DELETE FROM employee_working_hours
                WHERE employee_id = @EmployeeId;
                """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                CommandDefinition deactivateEmployeeCommand = new(
                    deactivateEmployeeSql,
                    new
                    {
                        EmployeeId = employeeId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(
                    deactivateEmployeeCommand);

                CommandDefinition deactivateAssignmentsCommand = new(
                    deactivateAssignmentsSql,
                    new
                    {
                        EmployeeId = employeeId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(
                    deactivateAssignmentsCommand);

                CommandDefinition deleteWorkingHoursCommand = new(
                    deleteWorkingHoursSql,
                    new
                    {
                        EmployeeId = employeeId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(
                    deleteWorkingHoursCommand);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

        }

        public async Task<PagedResult<Employee>> GetAllAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken)
        {
            const string sql = """
                    SELECT COUNT(*) 
                    FROM employees
                    WHERE 
                    (
                        @Search IS NULL
                        OR first_name ILIKE @SearchPattern
                        OR last_name ILIKE @SearchPattern
                        OR email ILIKE @SearchPattern
                    ) 
                    AND 
                    (
                        @IsActive IS NULL
                        OR is_active = @IsActive
                    );

                    SELECT
                        id,
                        first_name AS FirstName,
                        last_name AS LastName,
                        email AS Email,
                        phone AS Phone,
                        is_active AS IsActive,
                        created_at AS CreatedAt,
                        updated_at AS UpdatedAt
                    FROM employees
                    WHERE 
                    (
                        @Search IS NULL
                        OR first_name ILIKE @SearchPattern
                        OR last_name ILIKE @SearchPattern
                        OR email ILIKE @SearchPattern
                    )
                    AND 
                    (
                        @IsActive IS NULL
                        OR is_active = @IsActive
                    )
                    ORDER BY id
                    LIMIT @PageSize
                    OFFSET @Offset;
                    """;
                    
            var normalizedSearch  = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

            var parameters = new
            {
                Search = normalizedSearch,
                SearchPattern = normalizedSearch is null ? null : $"%{normalizedSearch}%",
                IsActive = isActive,
                PageSize = pageSize,
                Offset = (page - 1) * pageSize
            };

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

            var command = new CommandDefinition(
                sql,
                parameters,
                cancellationToken: cancellationToken);

            using var result = await connection.QueryMultipleAsync(command);

            var totalCount = await result.ReadSingleAsync<int>();

            var employees = (await result.ReadAsync<Employee>()).ToArray();

            return new PagedResult<Employee>
            {
                Items = employees,
                TotalCount = totalCount
            };
        }

        public async Task<Employee?> UpdateAsync(UpdateEmployeeData employee, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE employees
                SET 
                    first_name = @FirstName,
                    last_name = @LastName,
                    email = @Email,
                    phone = @Phone,
                    updated_at = NOW()
                WHERE id = @Id
                RETURNING
                    id,
                    first_name AS FirstName,
                    last_name AS LastName,
                    email AS Email,
                    phone AS Phone,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

                var command = new CommandDefinition(
                    sql,
                    employee,
                    cancellationToken: cancellationToken);

                return await connection.QuerySingleOrDefaultAsync<Employee>(command);
            } 
            catch(PostgresException exception)
                when (exception.SqlState == PostgresErrorCodes.UniqueViolation && exception.ConstraintName == "employees_email_key")
            {
                throw new EmployeeEmailAlreadyExistsException(employee.Email, exception);
            }
        }

        public async Task<Employee?> ActivateAsync(long id, CancellationToken cancellationToken)
        {
            var sql = """
                UPDATE employees
                SET 
                    updated_at = 
                        CASE 
                            WHEN is_active = FALSE THEN NOW()
                            ELSE updated_at
                        END,
                    is_active = TRUE
                WHERE id = @Id
                RETURNING
                    id,
                    first_name AS FirstName,
                    last_name AS LastName,
                    email AS Email,
                    phone AS Phone,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

            var command = new CommandDefinition(
                sql,
                new { Id = id},
                cancellationToken: cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<Employee>(command);
        }

    }
}
