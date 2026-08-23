using Dapper;
using Npgsql;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Exceptions;

namespace AppointmentBooking.Api.Repositories.EmployeeWorkingHoursRepository
{
    public sealed class EmployeeWorkingHoursRepository(NpgsqlDataSource dataSource) : IEmployeeWorkingHoursRepository
    {
        public async Task<EmployeeWorkingHours> CreateAsync(EmployeeWorkingHours employeeWorkingHours, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO employee_working_hours
                (
                    employee_id,
                    day_of_week,
                    starts_at,
                    ends_at
                )
                VALUES
                (
                    @EmployeeId,
                    @DayOfWeek,
                    @StartsAt,
                    @EndsAt
                )
                RETURNING
                    id,
                    employee_id AS EmployeeId,
                    day_of_week AS DayOfWeek,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                new
                {
                    employeeWorkingHours.EmployeeId,
                    DayOfWeek = (short)employeeWorkingHours.DayOfWeek,
                    StartsAt = employeeWorkingHours.StartsAt.ToTimeSpan(),
                    EndsAt = employeeWorkingHours.EndsAt.ToTimeSpan()
                },
                cancellationToken: cancellationToken);

            try
            {
                return await connection.QuerySingleAsync<EmployeeWorkingHours>(
                    command);
            }
            catch (PostgresException ex)
                when (ex.SqlState == "23P01")
            {
                throw new ConflictException($"Employee '{employeeWorkingHours.EmployeeId}' already has working hours that overlap this interval.");
            }
        }

        public async Task<EmployeeWorkingHours?> GetByIdAsync(long employeeId, long workingHoursId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    id,
                    employee_id AS EmployeeId,
                    day_of_week AS DayOfWeek,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt
                FROM employee_working_hours
                WHERE id = @WorkingHoursId
                    AND employee_id = @EmployeeId;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new(
                sql,
                new
                {
                    WorkingHoursId = workingHoursId,
                    EmployeeId = employeeId,
                },
                cancellationToken: cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<EmployeeWorkingHours>(
                command);
        }

        public async Task<IReadOnlyCollection<EmployeeWorkingHours>> GetByEmployeeIdAsync(long employeeId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    id,
                    employee_id AS EmployeeId,
                    day_of_week AS DayOfWeek,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt
                FROM employee_working_hours
                WHERE employee_id = @EmployeeId
                ORDER BY day_of_week, starts_at;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new(
                sql,
                new
                {
                    EmployeeId = employeeId
                },
                cancellationToken: cancellationToken);

            IEnumerable<EmployeeWorkingHours> result = await connection.QueryAsync<EmployeeWorkingHours>(command);

            return result.ToArray();
        }

        public async Task<EmployeeWorkingHours> UpdateAsync(long employeeId, long workingHoursId, EmployeeWorkingHours workingHours, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE employee_working_hours
                SET
                    day_of_week = @DayOfWeek,
                    starts_at = @StartsAt,
                    ends_at = @EndsAt
                WHERE id = @WorkingHoursId
                  AND employee_id = @EmployeeId
                RETURNING
                    id,
                    employee_id AS EmployeeId,
                    day_of_week AS DayOfWeek,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new(
               sql,
               new
               {
                   EmployeeId = employeeId,
                   WorkingHoursId = workingHoursId,
                   DayOfWeek = (short)workingHours.DayOfWeek,
                   StartsAt = workingHours.StartsAt.ToTimeSpan(),
                   EndsAt = workingHours.EndsAt.ToTimeSpan()
               },
               cancellationToken: cancellationToken);

            try
            {
                return await connection.QuerySingleAsync<EmployeeWorkingHours>(command);
            }
            catch (PostgresException ex)
                when (ex.SqlState == "23P01")
            {
                throw new ConflictException($"Employee '{employeeId}' already has working hours that overlap the updated interval.");
            }
        }

        public async Task DeleteAsync(long employeeId, long workingHoursId, CancellationToken cancellationToken)
        {
            const string sql = """
                DELETE FROM employee_working_hours
                WHERE id = @WorkingHoursId
                  AND employee_id = @EmployeeId;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new(
                sql,
                new
                {
                    EmployeeId = employeeId,
                    WorkingHoursId = workingHoursId
                },
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

    }
}
