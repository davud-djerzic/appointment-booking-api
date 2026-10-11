using Dapper;
using Npgsql;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models.Enums;
using System.Data;

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

        public async Task<IReadOnlyCollection<EmployeeWorkingHours>> ApplyRecurringBreakAsync(
            long employeeId,
            WeekDay dayOfWeek,
            TimeOnly startsAt,
            TimeOnly endsAt,
            CancellationToken cancellationToken)
        {
            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);
            await using NpgsqlTransaction transaction =
                await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

            IEnumerable<EmployeeWorkingHours> currentHours =
                await connection.QueryAsync<EmployeeWorkingHours>(
                    new CommandDefinition(
                        """
                        SELECT
                            id,
                            employee_id AS EmployeeId,
                            day_of_week AS DayOfWeek,
                            starts_at AS StartsAt,
                            ends_at AS EndsAt
                        FROM employee_working_hours
                        WHERE employee_id = @EmployeeId AND day_of_week = @DayOfWeek
                        ORDER BY starts_at
                        FOR UPDATE;
                        """,
                        new { EmployeeId = employeeId, DayOfWeek = (short)dayOfWeek },
                        transaction,
                        cancellationToken: cancellationToken));

            EmployeeWorkingHours[] dayHours = currentHours.ToArray();
            EmployeeWorkingHours? containingInterval = dayHours.FirstOrDefault(hour =>
                hour.StartsAt <= startsAt && hour.EndsAt >= endsAt);

            if (containingInterval is null)
            {
                throw new ConflictException(
                    "The recurring break must fit entirely inside one existing working interval.");
            }

            const string bookingConflictSql = """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM bookings b
                    WHERE b.employee_id = @EmployeeId
                      AND b.status = 'scheduled'
                      AND b.ends_at > NOW()
                      AND EXTRACT(ISODOW FROM b.starts_at AT TIME ZONE 'Europe/Sarajevo') = @DayOfWeek
                      AND (b.starts_at AT TIME ZONE 'Europe/Sarajevo')::time < @BreakEndsAt
                      AND (b.ends_at AT TIME ZONE 'Europe/Sarajevo')::time > @BreakStartsAt
                );
                """;

            bool hasConflictingBooking = await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    bookingConflictSql,
                    new
                    {
                        EmployeeId = employeeId,
                        DayOfWeek = (short)dayOfWeek,
                        BreakStartsAt = startsAt.ToTimeSpan(),
                        BreakEndsAt = endsAt.ToTimeSpan()
                    },
                    transaction,
                    cancellationToken: cancellationToken));

            if (hasConflictingBooking)
            {
                throw new ConflictException(
                    "The recurring break cannot be applied because a future booking already exists during that time.");
            }

            List<(TimeOnly StartsAt, TimeOnly EndsAt)> replacement = [];

            foreach (EmployeeWorkingHours hour in dayHours)
            {
                if (hour.Id != containingInterval.Id)
                {
                    replacement.Add((hour.StartsAt, hour.EndsAt));
                    continue;
                }

                if (hour.StartsAt < startsAt)
                {
                    replacement.Add((hour.StartsAt, startsAt));
                }

                if (endsAt < hour.EndsAt)
                {
                    replacement.Add((endsAt, hour.EndsAt));
                }
            }

            if (replacement.Count == 0)
            {
                throw new ConflictException(
                    "A recurring break cannot replace the employee's entire working day.");
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    DELETE FROM employee_working_hours
                    WHERE employee_id = @EmployeeId AND day_of_week = @DayOfWeek;
                    """,
                    new { EmployeeId = employeeId, DayOfWeek = (short)dayOfWeek },
                    transaction,
                    cancellationToken: cancellationToken));

            const string insertSql = """
                INSERT INTO employee_working_hours
                    (employee_id, day_of_week, starts_at, ends_at)
                VALUES
                    (@EmployeeId, @DayOfWeek, @StartsAt, @EndsAt);
                """;

            foreach ((TimeOnly workingStartsAt, TimeOnly workingEndsAt) in replacement)
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        insertSql,
                        new
                        {
                            EmployeeId = employeeId,
                            DayOfWeek = (short)dayOfWeek,
                            StartsAt = workingStartsAt.ToTimeSpan(),
                            EndsAt = workingEndsAt.ToTimeSpan()
                        },
                        transaction,
                        cancellationToken: cancellationToken));
            }

            IEnumerable<EmployeeWorkingHours> updatedHours =
                await connection.QueryAsync<EmployeeWorkingHours>(
                    new CommandDefinition(
                        """
                        SELECT
                            id,
                            employee_id AS EmployeeId,
                            day_of_week AS DayOfWeek,
                            starts_at AS StartsAt,
                            ends_at AS EndsAt
                        FROM employee_working_hours
                        WHERE employee_id = @EmployeeId
                        ORDER BY day_of_week, starts_at;
                        """,
                        new { EmployeeId = employeeId },
                        transaction,
                        cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);
            return updatedHours.ToArray();
        }

    }
}
