using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.BookedServices;
using Dapper;
using Npgsql;

namespace AppointmentBooking.Api.Repositories.Services
{
    public sealed class BookableServiceRepository(NpgsqlDataSource dataSource) : IBookableServiceRepository 
    {
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

            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

                var command = new CommandDefinition(
                    sql,
                    service,
                    cancellationToken: cancellationToken);

                return await connection.QuerySingleAsync<BookableService>(command);
            } catch (PostgresException ex)
                    when(ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                throw new ServiceNameAlreadyExistsException(service.Name, ex);
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

        public async Task<bool> DeactivateAsync(long id, CancellationToken cancellationToken)
        {
            const string sql = """
                    UPDATE services
                    SET 
                        updated_at = 
                            CASE 
                                WHEN is_active IS TRUE THEN NOW()
                                ELSE updated_at  
                            END,
                        is_active = FALSE
                    
                    WHERE id = @Id;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                new { Id = id },
                cancellationToken: cancellationToken);

            int affectedRows = await connection.ExecuteAsync(command);

            return affectedRows > 0;
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
            when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == "services_name_key")
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
                        END
                    is_active = TRUE
                WHERE id = @Id;
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

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
            CommandDefinition command = new CommandDefinition(
                sql,
                new { Id = id},
                cancellationToken: cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<BookableService>(command);
        }

    }
}
