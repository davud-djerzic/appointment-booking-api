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
            int firstAffectedIndex = -1;
            int lastAffectedIndex = -1;
            TimeOnly baseStartsAt = default;
            TimeOnly baseEndsAt = default;

            EmployeeWorkingHours? containingInterval = dayHours.FirstOrDefault(hour =>
                hour.StartsAt <= startsAt && hour.EndsAt >= endsAt);

            // Applying a recurring break splits one working interval into two rows.
            // On the next edit, treat those two rows as the original interval so the
            // break can be moved or adjusted across the old gap.
            if (dayHours.Length == 2 &&
                dayHours[0].StartsAt <= startsAt &&
                dayHours[1].EndsAt >= endsAt)
            {
                firstAffectedIndex = 0;
                lastAffectedIndex = 1;
                baseStartsAt = dayHours[0].StartsAt;
                baseEndsAt = dayHours[1].EndsAt;
            }
            else if (containingInterval is not null)
            {
                firstAffectedIndex = Array.IndexOf(dayHours, containingInterval);
                lastAffectedIndex = firstAffectedIndex;
                baseStartsAt = containingInterval.StartsAt;
                baseEndsAt = containingInterval.EndsAt;
            }
            else
            {
                // Support schedules with other intervals too: when the requested
                // break spans an existing gap, replace the adjacent pair around it.
                for (int index = 0; index < dayHours.Length - 1; index++)
                {
                    EmployeeWorkingHours before = dayHours[index];
                    EmployeeWorkingHours after = dayHours[index + 1];

                    if (before.EndsAt < after.StartsAt &&
                        before.StartsAt <= startsAt &&
                        after.EndsAt >= endsAt)
                    {
                        firstAffectedIndex = index;
                        lastAffectedIndex = index + 1;
                        baseStartsAt = before.StartsAt;
                        baseEndsAt = after.EndsAt;
                        break;
                    }
                }
            }

            if (firstAffectedIndex < 0)
            {
                throw new ConflictException(
                    "Stalna pauza mora biti unutar postojećeg radnog vremena.");
            }

            // Keep at least one working interval on both sides of the break.
            // Otherwise a break at the start or end of a workday is indistinguishable
            // from a shortened working schedule when pauses are displayed.
            if (startsAt <= baseStartsAt || endsAt >= baseEndsAt)
            {
                throw new ConflictException(
                    "Stalna pauza ne može biti na početku ni na kraju radnog vremena. Mora ostati radno vrijeme prije i poslije pauze.");
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

            for (int index = 0; index < dayHours.Length; index++)
            {
                if (index < firstAffectedIndex || index > lastAffectedIndex)
                {
                    EmployeeWorkingHours hour = dayHours[index];
                    replacement.Add((hour.StartsAt, hour.EndsAt));
                    continue;
                }

                if (index == firstAffectedIndex && baseStartsAt < startsAt)
                {
                    replacement.Add((baseStartsAt, startsAt));
                }

                if (index == firstAffectedIndex && endsAt < baseEndsAt)
                {
                    replacement.Add((endsAt, baseEndsAt));
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
