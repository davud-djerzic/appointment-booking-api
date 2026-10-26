using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.BookedServices;
using Dapper;
using Npgsql;

namespace AppointmentBooking.Api.Repositories.Services
{
    public sealed class BookableServiceRepository(NpgsqlDataSource dataSource) : IBookableServiceRepository 
    {
        // Unique index on lower(name) for active services only (Database/015_services_unique_active_name.sql).
        private const string ActiveNameUniqueIndex = "ux_services_active_name";

        // The salon's active employees provide every active service, so a service is
        // assigned to all active employees when it is created or re-activated.
        private const string AssignToActiveEmployeesSql = """
            INSERT INTO employee_services
            (
                employee_id,
                service_id
            )
            SELECT
                e.id,
                @ServiceId
            FROM employees AS e
            WHERE e.is_active = TRUE
            ON CONFLICT (employee_id, service_id)
            DO UPDATE
            SET
                updated_at =
                    CASE
                        WHEN employee_services.is_active = FALSE THEN NOW()
                        ELSE employee_services.updated_at
                    END,
                is_active = TRUE;
            """;

        public async Task<BookableService> CreateAsync(BookableService service, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO 
                    services (name, description, duration_minutes, price)
                VALUES 
                    (@Name, @Description, @DurationMinutes, @Price)
                RETURNING 
                    id,
                    name AS Name,
                    description AS Description,
                    duration_minutes AS DurationMinutes,
                    price AS Price,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                CommandDefinition command = new(
                    sql,
                    service,
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                BookableService created = await connection.QuerySingleAsync<BookableService>(command);

                await AssignToActiveEmployeesAsync(connection, transaction, created.Id, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return created;
            }
            catch (PostgresException ex)
                when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new ServiceNameAlreadyExistsException(service.Name, ex);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<BookableService?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT 
                    id,
                    name AS Name,
                    description AS Description,
                    duration_minutes AS DurationMinutes,
                    price AS Price,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM services
                WHERE id = @Id;
                """;

            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            
            var command = new CommandDefinition(
                sql,
                new { Id = id },
                cancellationToken: cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<BookableService>(command);
        }

        public async Task<bool> DeactivateAsync(
            long id,
            CancellationToken cancellationToken)
        {
            const string deactivateServiceSql = """
        UPDATE services
        SET
            updated_at =
                CASE
                    WHEN is_active = TRUE THEN NOW()
                    ELSE updated_at
                END,
            is_active = FALSE
        WHERE id = @Id;
        """;

            // A deleted service is removed from every employee. Nothing references
            // employee_services, so the rows can be deleted.
            const string deactivateAssignmentsSql = """
        DELETE FROM employee_services
        WHERE service_id = @Id;
        """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            await using NpgsqlTransaction transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                CommandDefinition deactivateServiceCommand = new(
                    deactivateServiceSql,
                    new { Id = id },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                int affectedRows =
                    await connection.ExecuteAsync(deactivateServiceCommand);

                if (affectedRows == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                CommandDefinition deactivateAssignmentsCommand = new(
                    deactivateAssignmentsSql,
                    new { Id = id },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(deactivateAssignmentsCommand);

                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<PagedResult<BookableService>> GetAllAsync(ServiceSearchCriteria criteria, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT COUNT(*)
                FROM services
                WHERE
                (
                    @Search IS NULL
                    OR name ILIKE @SearchPattern
                    OR description ILIKE @SearchPattern
                )
                AND
                (
                    @IsActive IS NULL
                    OR is_active = @IsActive
                )
                AND
                (
                    @MinPrice IS NULL
                    OR price >= @MinPrice
                )
                AND
                (
                    @MaxPrice IS NULL
                    OR price <= @MaxPrice
                )
                AND
                (
                    @MinDuration IS NULL
                    OR duration_minutes >= @MinDuration
                )
                AND
                (
                    @MaxDuration IS NULL
                    OR duration_minutes <= @MaxDuration
                );

                SELECT 
                    id,
                    name AS Name,
                    description AS Description,
                    duration_minutes AS DurationMinutes,
                    price AS Price,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM services
                WHERE
                (
                    @Search IS NULL
                    OR name ILIKE @SearchPattern
                    OR description ILIKE @SearchPattern
                )
                AND
                (
                    @IsActive IS NULL
                    OR is_active = @IsActive
                )
                AND
                (
                    @MinPrice IS NULL
                    OR price >= @MinPrice
                )
                AND
                (
                    @MaxPrice IS NULL
                    OR price <= @MaxPrice
                )
                AND
                (
                    @MinDuration IS NULL
                    OR duration_minutes >= @MinDuration
                )
                AND
                (
                    @MaxDuration IS NULL
                    OR duration_minutes <= @MaxDuration
                )
                ORDER BY id
                LIMIT @PageSize
                OFFSET @Offset;
                """;

            var normalizedSearch = string.IsNullOrWhiteSpace(criteria.Search) ? null : criteria.Search.Trim();

            var parameters = new
            {
                Search = normalizedSearch,
                SearchPattern = normalizedSearch is null ? null : $"%{normalizedSearch}%",
                IsActive = criteria.IsActive,
                MinPrice = criteria.MinPrice,
                MaxPrice = criteria.MaxPrice,
                MinDuration = criteria.MinDuration,
                MaxDuration = criteria.MaxDuration,
                PageSize = criteria.PageSize,
                Offset = (criteria.Page - 1) * criteria.PageSize
            };

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                parameters,
                cancellationToken: cancellationToken);

            using var result = await connection.QueryMultipleAsync(command);

            var totalCount = await result.ReadSingleAsync<int>();

            var services = (await result.ReadAsync<BookableService>()).ToArray();

            return new PagedResult<BookableService>
            {
                Items = services,
                TotalCount = totalCount
            };
        }

        public async Task<BookableService?> UpdateAsync(UpdateServiceData service, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE services
                SET 
                    name = @Name,
                    description = @Description,
                    duration_minutes = @DurationMinutes,
                    price = @Price,
                    updated_at = NOW()
                WHERE id = @Id
                RETURNING
                    id,
                    name AS Name,
                    description AS Description,
                    duration_minutes AS DurationMinutes,
                    price AS Price,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            try
            {
                await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
                CommandDefinition command = new CommandDefinition(
                    sql,
                    service,
                    cancellationToken: cancellationToken);

                return await connection.QuerySingleOrDefaultAsync<BookableService>(command);
            }
            catch (PostgresException ex)
            when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == ActiveNameUniqueIndex)
            {
                throw new ServiceNameAlreadyExistsException(service.Name, ex);
            }
        }

        public async Task<BookableService?> ActivateAsync(long id, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE services
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
                    name AS Name,
                    description AS Description,
                    duration_minutes AS DurationMinutes,
                    price AS Price,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                CommandDefinition command = new(
                    sql,
                    new { Id = id },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                BookableService? activated = await connection.QuerySingleOrDefaultAsync<BookableService>(command);
                if (activated is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                await AssignToActiveEmployeesAsync(connection, transaction, activated.Id, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return activated;
            }
            catch (PostgresException ex)
                when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == ActiveNameUniqueIndex)
            {
                // Another active service already uses this name.
                await transaction.RollbackAsync(cancellationToken);
                BookableService? existing = await GetByIdAsync(id, cancellationToken);
                throw new ServiceNameAlreadyExistsException(existing?.Name ?? $"#{id}", ex);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static Task<int> AssignToActiveEmployeesAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long serviceId,
            CancellationToken cancellationToken)
        {
            CommandDefinition command = new(
                AssignToActiveEmployeesSql,
                new { ServiceId = serviceId },
                transaction: transaction,
                cancellationToken: cancellationToken);

            return connection.ExecuteAsync(command);
        }

        public async Task<IReadOnlyCollection<BookableService>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken)
        {
            if (ids.Count == 0)
            {
                return [];
            }

            const string sql = """
                SELECT
                    id,
                    name AS Name,
                    description AS Description,
                    duration_minutes AS DurationMinutes,
                    price AS Price,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM services
                WHERE id = ANY(@ServiceIds);
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            IEnumerable<BookableService> services =
                await connection.QueryAsync<BookableService>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            ServiceIds = ids.ToArray()
                        },
                        cancellationToken: cancellationToken));

            return services.ToArray();
        }
    }
}
