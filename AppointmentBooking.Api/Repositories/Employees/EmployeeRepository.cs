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
                    updated_at AS UpdatedAt
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

        public async Task<bool> DeactivateAsync(long id, CancellationToken cancellationToken)
        {
            const string sql = """
                    UPDATE employees 
                    SET 
                        updated_at =
                        CASE 
                            WHEN is_active = TRUE THEN NOW()
                            ELSE updated_at
                        END,
                        is_active = FALSE
                    WHERE id = @Id;
                    """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

            var command = new CommandDefinition(
                sql,
                new { Id = id },
                cancellationToken: cancellationToken);

            var affectedRows = await connection.ExecuteAsync(command);

            return affectedRows > 0;
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

        public async Task<Employee?> UpdateAsync(Employee employee, CancellationToken cancellationToken)
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
