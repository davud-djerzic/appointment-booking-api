using AppointmentBooking.Api.Models;
using Dapper;
using Npgsql;

namespace AppointmentBooking.Api.Repositories.EmployeeAvailabilityBlocks
{
    public sealed class EmployeeUnavailabilityRepository(NpgsqlDataSource dataSource)
        : IEmployeeUnavailabilityRepository
    {
        public async Task<EmployeeUnavailability> CreateAsync(
            EmployeeUnavailability unavailability,
            CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO employee_unavailability
                (
                    employee_id,
                    block_type,
                    starts_at,
                    ends_at,
                    is_all_day,
                    note
                )
                VALUES
                (
                    @EmployeeId,
                    @Type,
                    @StartsAt,
                    @EndsAt,
                    @IsAllDay,
                    @Note
                )
                RETURNING
                    id,
                    employee_id AS EmployeeId,
                    block_type AS Type,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt,
                    is_all_day AS IsAllDay,
                    note AS Note,
                    created_at AS CreatedAt;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            return await connection.QuerySingleAsync<EmployeeUnavailability>(
                new CommandDefinition(
                    sql,
                    ToParameters(unavailability),
                    cancellationToken: cancellationToken));
        }

        public async Task<EmployeeUnavailability?> GetByIdAsync(
            long employeeId,
            long unavailabilityId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    id,
                    employee_id AS EmployeeId,
                    block_type AS Type,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt,
                    is_all_day AS IsAllDay,
                    note AS Note,
                    created_at AS CreatedAt
                FROM employee_unavailability
                WHERE employee_id = @EmployeeId
                  AND id = @UnavailabilityId;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<EmployeeUnavailability>(
                new CommandDefinition(
                    sql,
                    new { EmployeeId = employeeId, UnavailabilityId = unavailabilityId },
                    cancellationToken: cancellationToken));
        }

        public async Task<IReadOnlyCollection<EmployeeUnavailability>> GetByEmployeeIdAsync(
            long employeeId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    id,
                    employee_id AS EmployeeId,
                    block_type AS Type,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt,
                    is_all_day AS IsAllDay,
                    note AS Note,
                    created_at AS CreatedAt
                FROM employee_unavailability
                WHERE employee_id = @EmployeeId
                ORDER BY starts_at, id;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            IEnumerable<EmployeeUnavailability> records =
                await connection.QueryAsync<EmployeeUnavailability>(
                    new CommandDefinition(
                        sql,
                        new { EmployeeId = employeeId },
                        cancellationToken: cancellationToken));

            return records.ToArray();
        }

        public async Task<IReadOnlyCollection<EmployeeUnavailability>> GetOverlappingAsync(
            long employeeId,
            DateTimeOffset startsAt,
            DateTimeOffset endsAt,
            CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    id,
                    employee_id AS EmployeeId,
                    block_type AS Type,
                    date_trunc('minute', starts_at) AS StartsAt,
                    date_trunc('minute', ends_at) AS EndsAt,
                    is_all_day AS IsAllDay,
                    note AS Note,
                    created_at AS CreatedAt
                FROM employee_unavailability
                WHERE employee_id = @EmployeeId
                  AND date_trunc('minute', starts_at) < @EndsAt
                  AND date_trunc('minute', ends_at) > @StartsAt
                ORDER BY starts_at, id;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            IEnumerable<EmployeeUnavailability> records =
                await connection.QueryAsync<EmployeeUnavailability>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            EmployeeId = employeeId,
                            StartsAt = startsAt.ToUniversalTime(),
                            EndsAt = endsAt.ToUniversalTime()
                        },
                        cancellationToken: cancellationToken));

            return records.ToArray();
        }

        public async Task<EmployeeUnavailability?> UpdateAsync(
            EmployeeUnavailability unavailability,
            CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE employee_unavailability
                SET
                    block_type = @Type,
                    starts_at = @StartsAt,
                    ends_at = @EndsAt,
                    is_all_day = @IsAllDay,
                    note = @Note
                WHERE employee_id = @EmployeeId
                  AND id = @Id
                RETURNING
                    id,
                    employee_id AS EmployeeId,
                    block_type AS Type,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt,
                    is_all_day AS IsAllDay,
                    note AS Note,
                    created_at AS CreatedAt;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<EmployeeUnavailability>(
                new CommandDefinition(
                    sql,
                    ToParameters(unavailability),
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> DeleteAsync(
            long employeeId,
            long unavailabilityId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                DELETE FROM employee_unavailability
                WHERE employee_id = @EmployeeId
                  AND id = @UnavailabilityId;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            int affectedRows = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { EmployeeId = employeeId, UnavailabilityId = unavailabilityId },
                    cancellationToken: cancellationToken));

            return affectedRows > 0;
        }

        private static object ToParameters(EmployeeUnavailability unavailability)
        {
            return new
            {
                unavailability.Id,
                unavailability.EmployeeId,
                Type = (short)unavailability.Type,
                StartsAt = unavailability.StartsAt.ToUniversalTime(),
                EndsAt = unavailability.EndsAt.ToUniversalTime(),
                unavailability.IsAllDay,
                unavailability.Note
            };
        }
    }
}
