using AppointmentBooking.Api.Models;
using Dapper;
using Npgsql;
using AppointmentBooking.Api.Exceptions;

namespace AppointmentBooking.Api.Repositories.Employees
{
    public sealed class EmployeeRepository(NpgsqlDataSource dataSource) : IEmployeeRepository
    {
        public async Task<Employee> CreateEmployeeAccountAsync(UserAccount userAccount, string phone, CancellationToken cancellationToken)
        {
            const string createUserAccountSql = """
                INSERT INTO user_accounts
                (
                    first_name,
                    last_name,
                    email,
                    password_hash,
                    role,
                    is_active
                )
                VALUES
                (
                    @FirstName,
                    @LastName,
                    @Email,
                    @PasswordHash,
                    @Role,
                    @IsActive
                )
                RETURNING
                    id,
                    first_name AS FirstName,
                    last_name AS LastName,
                    email,
                    password_hash AS PasswordHash,
                    role,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            const string createEmployeeSql = """
                INSERT INTO employees
                (
                    user_account_id,
                    phone
                )
                VALUES
                (
                    @UserAccountId,
                    @Phone
                )
                RETURNING
                    id,
                    user_account_id AS UserAccountId,
                    phone AS Phone,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            await using NpgsqlConnection connection =
               await dataSource.OpenConnectionAsync(cancellationToken);

            await using NpgsqlTransaction transaction =
                await connection.BeginTransactionAsync(cancellationToken);


            try
            {
                UserAccount createdUserAccount =
                    await connection.QuerySingleAsync<UserAccount>(
                        new CommandDefinition(
                            createUserAccountSql,
                            new
                            {
                                userAccount.FirstName,
                                userAccount.LastName,
                                userAccount.Email,
                                userAccount.PasswordHash,
                                Role = userAccount.Role
                                    .ToString()
                                    .ToLowerInvariant(),
                                userAccount.IsActive
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                Employee createdEmployee =
                    await connection.QuerySingleAsync<Employee>(
                        new CommandDefinition(
                            createEmployeeSql,
                            new
                            {
                                UserAccountId = createdUserAccount.Id,
                                Phone = phone
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                return createdEmployee;
            }
            catch (PostgresException ex)
                when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                await transaction.RollbackAsync(cancellationToken);

                if (ex.ConstraintName == "uq_user_accounts_email")
                {
                    throw new ConflictException(
                        "An account with this email already exists.");
                }

                throw;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Employee?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    id,
                    user_account_id AS UserAccountId,
                    phone AS Phone,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM employees
                WHERE id = @Id;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new(
                sql,
                new { Id = id },
                cancellationToken: cancellationToken);

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

            const string deactivateUserAccountSql = """
                UPDATE user_accounts
                SET
                    updated_at =
                        CASE
                            WHEN is_active = TRUE THEN NOW()
                            ELSE updated_at
                        END,
                    is_active = FALSE
                WHERE id =
                (
                    SELECT user_account_id
                    FROM employees
                    WHERE id = @EmployeeId
                );
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

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            await using NpgsqlTransaction transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        deactivateEmployeeSql,
                        new
                        {
                            EmployeeId = employeeId
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        deactivateUserAccountSql,
                        new
                        {
                            EmployeeId = employeeId
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        deactivateAssignmentsSql,
                        new
                        {
                            EmployeeId = employeeId
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        deleteWorkingHoursSql,
                        new
                        {
                            EmployeeId = employeeId
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

        }

        public async Task<PagedResult<EmployeeListItem>> GetAllAsync(
            string? search,
            bool? isActive,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT COUNT(*)
                FROM employees e
                INNER JOIN user_accounts ua
                    ON e.user_account_id = ua.id
                WHERE
                (
                    @Search IS NULL
                    OR ua.first_name ILIKE @SearchPattern
                    OR ua.last_name ILIKE @SearchPattern
                    OR ua.email ILIKE @SearchPattern
                )
                AND
                (
                    @IsActive IS NULL
                    OR e.is_active = @IsActive
                );

                SELECT
                    e.id AS Id,
                    ua.first_name AS FirstName,
                    ua.last_name AS LastName,
                    ua.email AS Email,
                    e.phone AS Phone,
                    e.is_active AS IsActive,
                    e.created_at AS CreatedAt,
                    e.updated_at AS UpdatedAt
                FROM employees e
                INNER JOIN user_accounts ua
                    ON e.user_account_id = ua.id
                WHERE
                (
                    @Search IS NULL
                    OR ua.first_name ILIKE @SearchPattern
                    OR ua.last_name ILIKE @SearchPattern
                    OR ua.email ILIKE @SearchPattern
                )
                AND
                (
                    @IsActive IS NULL
                    OR e.is_active = @IsActive
                )
                ORDER BY e.id
                LIMIT @PageSize
                OFFSET @Offset;
                """;

            string? normalizedSearch =
                string.IsNullOrWhiteSpace(search)
                    ? null
                    : search.Trim();

            var parameters = new
            {
                Search = normalizedSearch,
                SearchPattern =
                    normalizedSearch is null
                        ? null
                        : $"%{normalizedSearch}%",

                IsActive = isActive,
                PageSize = pageSize,
                Offset = (page - 1) * pageSize
            };

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(
                    cancellationToken);

            CommandDefinition command = new(
                sql,
                parameters,
                cancellationToken: cancellationToken);

            using var result =
                await connection.QueryMultipleAsync(command);

            int totalCount =
                await result.ReadSingleAsync<int>();

            EmployeeListItem[] employees =
                (await result.ReadAsync<EmployeeListItem>())
                .ToArray();

            return new PagedResult<EmployeeListItem>
            {
                Items = employees,
                TotalCount = totalCount
            };
        }

        public async Task<Employee> UpdateAsync(UpdateEmployeeData employee, CancellationToken cancellationToken)
        {
            const string updateUserAccountSql = """
                UPDATE user_accounts
                SET
                    first_name = @FirstName,
                    last_name = @LastName,
                    email = @Email,
                    updated_at = NOW()
                WHERE id = (
                    SELECT user_account_id
                    FROM employees
                    WHERE id = @Id
                )
                RETURNING
                    id;
                """;

            const string updateEmployeeSql = """
                UPDATE employees
                SET
                    phone = @Phone,
                    updated_at = NOW()
                WHERE id = @Id
                RETURNING
                    id,
                    user_account_id AS UserAccountId,
                    phone AS Phone,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        updateUserAccountSql,
                        new
                        {
                            employee.FirstName,
                            employee.LastName,
                            employee.Email,
                            employee.Id
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                Employee updatedEmployee =
                    await connection.QuerySingleAsync<Employee>(
                        new CommandDefinition(
                            updateEmployeeSql,
                            new
                            {
                                employee.Id,
                                employee.Phone
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                return updatedEmployee;
            }
            catch (PostgresException exception)
                when (
                    exception.SqlState == PostgresErrorCodes.UniqueViolation &&
                    exception.ConstraintName == "uq_user_accounts_email")
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new ConflictException("An account with this email already exists.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<Employee> ActivateAsync(long id, CancellationToken cancellationToken)
        {
            const string activateUserAccountSql = """
                UPDATE user_accounts
                SET
                    updated_at =
                        CASE
                            WHEN is_active = FALSE THEN NOW()
                            ELSE updated_at
                        END,
                    is_active = TRUE
                WHERE id =
                (
                    SELECT user_account_id
                    FROM employees
                    WHERE id = @Id
                );
                """;

            const string activateEmployeeSql = """
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
                    user_account_id AS UserAccountId,
                    phone AS Phone,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            await using NpgsqlTransaction transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        activateUserAccountSql,
                        new { Id = id },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                Employee activatedEmployee =
                    await connection.QuerySingleAsync<Employee>(
                        new CommandDefinition(
                            activateEmployeeSql,
                            new { Id = id },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                return activatedEmployee;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }


        public async Task<Employee?> GetByUserAccountIdAsync(long userAccountId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    id,
                    user_account_id AS UserAccountId,
                    phone AS Phone,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM employees
                WHERE user_account_id = @UserAccountId;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(
                    cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<Employee>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        UserAccountId = userAccountId
                    },
                    cancellationToken: cancellationToken));
        }
    }
}
