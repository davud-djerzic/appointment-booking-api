using AppointmentBooking.Api.DTOs.Bookings.Response;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Models.Enums;
using Dapper;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Npgsql;
using static Dapper.SqlMapper;

namespace AppointmentBooking.Api.Repositories.Bookings
{
    public sealed class BookingRepository(NpgsqlDataSource dataSource) : IBookingRepository
    {
        public async Task<bool> HasScheduledOverlapAsync(long employeeId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT EXISTS
                (
                    SELECT 1
                    FROM bookings
                    WHERE employee_id = @EmployeeId
                      AND status = 'scheduled'
                      AND starts_at < @EndsAt
                      AND ends_at > @StartsAt
                );
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(
                    cancellationToken);

            return await connection.ExecuteScalarAsync<bool>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        EmployeeId = employeeId,
                        StartsAt = startsAt,
                        EndsAt = endsAt
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<BookingCreationResult> CreateAsync(Booking booking, IReadOnlyList<Appointment> appointments, CancellationToken cancellationToken)
        {
            const string createBookingSql = """
                INSERT INTO bookings
                (
                    customer_id,
                    employee_id,
                    starts_at,
                    ends_at,
                    status,
                    notes
                )
                VALUES
                (
                    @CustomerId,
                    @EmployeeId,
                    @StartsAt,
                    @EndsAt,
                    @Status,
                    @Notes
                )
                RETURNING
                    id,
                    customer_id AS CustomerId,
                    employee_id AS EmployeeId,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt,
                    status AS Status,
                    notes AS Notes,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt,
                    completed_at AS CompletedAt,
                    completion_source AS CompletionSource;
                """;

            const string createAppointmentSql = """
                INSERT INTO appointments
                (
                    booking_id,
                    service_id,
                    service_name_at_booking,
                    duration_minutes_at_booking,
                    price_at_booking,
                    starts_at,
                    ends_at,
                    status
                )
                VALUES
                (
                    @BookingId,
                    @ServiceId,
                    @ServiceNameAtBooking,
                    @DurationMinutesAtBooking,
                    @PriceAtBooking,
                    @StartsAt,
                    @EndsAt,
                    @Status
                )
                RETURNING
                    id,
                    booking_id AS BookingId,
                    service_id AS ServiceId,
                    service_name_at_booking AS ServiceNameAtBooking,
                    duration_minutes_at_booking AS DurationMinutesAtBooking,
                    price_at_booking AS PriceAtBooking,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt,
                    status AS Status,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            await using NpgsqlConnection connection =
               await dataSource.OpenConnectionAsync(cancellationToken);

            await using NpgsqlTransaction transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                Booking createdBooking =
                    await connection.QuerySingleAsync<Booking>(
                        new CommandDefinition(
                            createBookingSql,
                            new
                            {
                                booking.CustomerId,
                                booking.EmployeeId,
                                booking.StartsAt,
                                booking.EndsAt,
                                Status = booking.Status
                                    .ToString()
                                    .ToLowerInvariant(),
                                booking.Notes
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                List<Appointment> createdAppointments = new(appointments.Count);

                foreach (Appointment appointment in appointments)
                {
                    Appointment createdAppointment =
                        await connection.QuerySingleAsync<Appointment>(
                            new CommandDefinition(
                                createAppointmentSql,
                                new
                                {
                                    BookingId = createdBooking.Id,
                                    appointment.ServiceId,
                                    appointment.ServiceNameAtBooking,
                                    appointment.DurationMinutesAtBooking,
                                    appointment.PriceAtBooking,
                                    appointment.StartsAt,
                                    appointment.EndsAt,
                                    Status = appointment.Status
                                        .ToString()
                                        .ToLowerInvariant()
                                },
                                transaction: transaction,
                                cancellationToken: cancellationToken));

                    createdAppointments.Add(createdAppointment);
                }

                await transaction.CommitAsync(cancellationToken);

                return new BookingCreationResult(createdBooking, createdAppointments);
            }
            catch (PostgresException ex)
                when (ex.SqlState == PostgresErrorCodes.ExclusionViolation &&
                      ex.ConstraintName == "ex_bookings_employee_time")
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new ConflictException(
                    "The employee is already booked for the selected time.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<BookingReadResult?> GetByIdAsync(long bookingId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    b.id AS Id,
                    b.customer_id AS CustomerId,
                    b.employee_id AS EmployeeId,
                    b.starts_at AS StartsAt,
                    b.ends_at AS EndsAt,
                    b.status AS Status,
                    b.notes AS Notes,
                    b.created_at AS CreatedAt,
                    b.updated_at AS UpdatedAt,

                    b.completed_at AS CompletedAt,
                    b.completion_source AS CompletionSource,

                    ua.first_name AS EmployeeFirstName,
                    ua.last_name AS EmployeeLastName
                FROM bookings AS b
                INNER JOIN employees AS e
                    ON b.employee_id = e.id
                INNER JOIN user_accounts AS ua
                    ON ua.id = e.user_account_id
                WHERE b.id = @BookingId;

                SELECT 
                    id AS Id,
                    booking_id AS BookingId,
                    service_id AS ServiceId,
                    service_name_at_booking AS ServiceNameAtBooking,
                    duration_minutes_at_booking AS DurationMinutesAtBooking,
                    price_at_booking AS PriceAtBooking,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt,
                    status AS Status,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM appointments
                WHERE booking_id = @BookingId
                ORDER BY starts_at;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            using GridReader result = await connection.QueryMultipleAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        BookingId = bookingId
                    },
                    cancellationToken: cancellationToken));

            BookingReadRow? bookingRow = await result.ReadSingleOrDefaultAsync<BookingReadRow>();
            if (bookingRow is null) return null;

            Appointment[] appointments = (await result.ReadAsync<Appointment>()).ToArray();

            Booking booking = new()
            {
                Id = bookingRow.Id,
                CustomerId = bookingRow.CustomerId,
                EmployeeId = bookingRow.EmployeeId,
                StartsAt = bookingRow.StartsAt,
                EndsAt = bookingRow.EndsAt,
                Status = bookingRow.Status,
                Notes = bookingRow.Notes,
                CreatedAt = bookingRow.CreatedAt,
                UpdatedAt = bookingRow.UpdatedAt,
                CompletedAt = bookingRow.CompletedAt,
                CompletionSource = bookingRow.CompletionSource
            };

            return new BookingReadResult(booking, bookingRow.EmployeeFirstName, bookingRow.EmployeeLastName,appointments);
        }

        public async Task<PagedResult<BookingListItem>> GetCustomerBookingsAsync(long customerId, BookingSearchCriteria criteria, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT COUNT(*)
                FROM bookings AS b
                WHERE b.customer_id = @CustomerId
                  AND (
                        @Status IS NULL
                        OR b.status = @Status
                      )
                  AND (
                        @From IS NULL
                        OR b.starts_at >= @From
                      )
                  AND (
                        @To IS NULL
                        OR b.starts_at <= @To
                      );

                SELECT
                    b.id AS Id,
                    b.customer_id AS CustomerId,
                    b.employee_id AS EmployeeId,

                    ua.first_name AS EmployeeFirstName,
                    ua.last_name AS EmployeeLastName,

                    b.starts_at AS StartsAt,
                    b.ends_at AS EndsAt,

                    COALESCE(
                        SUM(a.duration_minutes_at_booking),
                        0
                    )::integer AS TotalDurationMinutes,

                    COALESCE(
                        SUM(a.price_at_booking),
                        0
                    ) AS TotalPrice,

                    b.status AS Status,
                    b.notes AS Notes,
                    b.created_at AS CreatedAt,
                    b.updated_at AS UpdatedAt

                FROM
                (
                    SELECT
                        id,
                        customer_id,
                        employee_id,
                        starts_at,
                        ends_at,
                        status,
                        notes,
                        created_at,
                        updated_at
                    FROM bookings
                    WHERE customer_id = @CustomerId
                      AND (
                            @Status IS NULL
                            OR status = @Status
                          )
                      AND (
                            @From IS NULL
                            OR starts_at >= @From
                          )
                      AND (
                            @To IS NULL
                            OR starts_at <= @To
                          )
                    ORDER BY starts_at DESC, id DESC
                    LIMIT @PageSize
                    OFFSET @Offset
                ) AS b

                INNER JOIN employees AS e
                    ON e.id = b.employee_id

                INNER JOIN user_accounts AS ua
                    ON ua.id = e.user_account_id

                LEFT JOIN appointments AS a
                    ON a.booking_id = b.id

                GROUP BY
                    b.id,
                    b.customer_id,
                    b.employee_id,
                    ua.first_name,
                    ua.last_name,
                    b.starts_at,
                    b.ends_at,
                    b.status,
                    b.notes,
                    b.created_at,
                    b.updated_at

                ORDER BY
                    b.starts_at DESC,
                    b.id DESC;
                """;

            DateTimeOffset? fromUtc = criteria.From?.ToUniversalTime();

            DateTimeOffset? toUtc = criteria.To?.ToUniversalTime();

            string? status = criteria.Status?.ToString().ToLowerInvariant();

            int offset =
                (criteria.Page - 1) * criteria.PageSize;

            var parameters = new
            {
                CustomerId = customerId,
                Status = status,
                From = fromUtc,
                To = toUtc,
                PageSize = criteria.PageSize,
                Offset = offset
            };

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new(
                sql,
                parameters,
                cancellationToken: cancellationToken);

            using GridReader result = await connection.QueryMultipleAsync(command);

            int totalCount = await result.ReadSingleAsync<int>();

            BookingListItem[] items = (await result.ReadAsync<BookingListItem>()).ToArray();

            return new PagedResult<BookingListItem>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<bool> CancelAsync(long bookingId, CancellationToken cancellationToken)
        {
            const string cancelBookingSql = """
                UPDATE bookings
                SET
                    status = 'cancelled',
                    updated_at = NOW()
                WHERE id = @BookingId
                  AND 
                  (
                    status = 'scheduled'
                    OR
                    (
                        status = 'completed'
                        AND completion_source = 'automatic'
                    )
                  );
                """;

            const string cancelAppointmentsSql = """
                UPDATE appointments
                SET
                    status = 'cancelled',
                    updated_at = NOW()
                WHERE booking_id = @BookingId
                  AND status IN ('scheduled', 'completed');
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            await using NpgsqlTransaction transaction =await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await connection.ExecuteAsync(
                        new CommandDefinition(
                            cancelBookingSql,
                            new
                            {
                                BookingId = bookingId
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                if (affectedRows == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return false;
                }

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        cancelAppointmentsSql,
                        new
                        {
                            BookingId = bookingId
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }

        public async Task<bool> CompleteAsync(long bookingId, CancellationToken cancellationToken)
        {
            const string completeBookingSql = """
                UPDATE bookings
                    SET
                        status = 'completed',
                        completion_source = 'manual',
                        completed_at = NOW(),
                        updated_at = NOW()
                    WHERE id = @BookingId
                      AND status = 'scheduled'
                      AND ends_at <= NOW();
                """;

            const string completeAppointmentsSql = """
                 UPDATE appointments
                    SET
                        status = 'completed',
                        updated_at = NOW()
                    WHERE booking_id = @BookingId
                      AND status = 'scheduled';
                 """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                int affectedRows = await connection.ExecuteAsync(
                        new CommandDefinition(
                            completeBookingSql,
                            new
                            {
                                BookingId = bookingId
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                if (affectedRows == 0)
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    return false;
                }

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        completeAppointmentsSql,
                        new
                        {
                            BookingId = bookingId
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                throw;
            }
        }

        public async Task<int> CompleteExpiredAutomaticallyAsync(DateTimeOffset localNow, CancellationToken cancellationToken)
        {

            DateTimeOffset startOfLocalDay = new DateTimeOffset(localNow.Date, localNow.Offset);

            DateTimeOffset startOfNextLocalDay = startOfLocalDay.AddDays(1);

            DateTimeOffset nowUtc = localNow.ToUniversalTime();

            DateTimeOffset startOfDayUtc = startOfLocalDay.ToUniversalTime();

            DateTimeOffset startOfNextDayUtc = startOfNextLocalDay.ToUniversalTime();

            const string completeBookingsSql = """
                UPDATE bookings
                SET
                    status = 'completed',
                    completion_source = 'automatic',
                    completed_at = @CompletedAt,
                    updated_at = @CompletedAt
                WHERE status = 'scheduled'
                  AND starts_at >= @StartOfDay
                AND starts_at < @StartOfNextDay
                AND ends_at <= @CompletedAt
                RETURNING id;
                """;

            const string completeAppointmentsSql = """
                UPDATE appointments
                SET
                    status = 'completed',
                    updated_at = @CompletedAt
                WHERE booking_id = ANY(@BookingIds)
                  AND status = 'scheduled';
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                long[] bookingIds =
                    (await connection.QueryAsync<long>(
                        new CommandDefinition(
                            completeBookingsSql,
                            new
                            {
                                CompletedAt = nowUtc,
                                StartOfDay = startOfDayUtc,
                                StartOfNextDay = startOfNextDayUtc
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken)))
                    .ToArray();

                if (bookingIds.Length == 0)
                {
                    await transaction.CommitAsync(
                        cancellationToken);

                    return 0;
                }

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        completeAppointmentsSql,
                        new
                        {
                            CompletedAt = nowUtc,
                            BookingIds = bookingIds
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(
                    cancellationToken);

                return bookingIds.Length;
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }

        public async Task<PagedResult<EmployeeBookingListItem>> GetEmployeeBookingsAsync(long employeeId, BookingSearchCriteria criteria, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT COUNT(*)
                FROM bookings AS b
                WHERE b.employee_id = @EmployeeId
                    AND (
                            @Status IS NULL
                            OR b.status = @Status
                        )
                    AND (
                            @From IS NULL
                            OR b.starts_at >= @From
                        )
                    AND (
                            @To IS NULL
                            OR b.starts_at <= @To
                        );
                SELECT
                    b.id AS Id,
                    b.customer_id AS CustomerId,

                    ua.first_name AS CustomerFirstName,
                    ua.last_name AS CustomerLastName,

                    b.starts_at AS StartsAt,
                    b.ends_at AS EndsAt,

                    COALESCE(
                        SUM(a.duration_minutes_at_booking), 0
                    )::integer AS TotalDurationMinutes,

                    COALESCE(
                        SUM(a.price_at_booking), 0
                    ) AS TotalPrice,

                    b.status AS Status,
                    b.notes AS Notes,
                    b.created_at AS CreatedAt,
                    b.updated_at AS UpdatedAt,
                    ua.email AS CustomerEmail,
                    c.phone AS CustomerPhone
                FROM
                (
                    SELECT
                        id,
                        customer_id,
                        employee_id,
                        starts_at,
                        ends_at,
                        status,
                        notes,
                        created_at,
                        updated_at
                    FROM bookings
                    WHERE employee_id = @EmployeeId
                      AND (
                            @Status IS NULL
                            OR status = @Status
                          )
                      AND (
                            @From IS NULL
                            OR starts_at >= @From
                          )
                      AND (
                            @To IS NULL
                            OR starts_at <= @To
                          )
                    ORDER BY
                        starts_at DESC,
                        id DESC
                    LIMIT @PageSize
                    OFFSET @Offset
                ) AS b

                INNER JOIN customers AS c
                    ON c.id = b.customer_id

                INNER JOIN user_accounts AS ua
                    ON ua.id = c.user_account_id

                LEFT JOIN appointments AS a
                    ON a.booking_id = b.id

                GROUP BY
                    b.id,
                    b.customer_id,
                    ua.first_name,
                    ua.last_name,
                    b.starts_at,
                    b.ends_at,
                    b.status,
                    b.notes,
                    b.created_at,
                    b.updated_at,
                    ua.email,
                    c.phone

                ORDER BY
                    b.starts_at DESC,
                    b.id DESC;
                """;

            DateTimeOffset? fromUtc =
        criteria.From?.ToUniversalTime();

            DateTimeOffset? toUtc =
                criteria.To?.ToUniversalTime();

            string? status =
                criteria.Status?
                    .ToString()
                    .ToLowerInvariant();

            int offset =
                (criteria.Page - 1) *
                criteria.PageSize;

            var parameters = new
            {
                EmployeeId = employeeId,
                Status = status,
                From = fromUtc,
                To = toUtc,
                PageSize = criteria.PageSize,
                Offset = offset
            };

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(
                    cancellationToken);

            CommandDefinition command = new(
                sql,
                parameters,
                cancellationToken: cancellationToken);

            using GridReader result =
                await connection.QueryMultipleAsync(command);

            int totalCount =
                await result.ReadSingleAsync<int>();

            EmployeeBookingListItem[] items =
                (await result.ReadAsync<EmployeeBookingListItem>())
                .ToArray();

            return new PagedResult<EmployeeBookingListItem>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<IReadOnlyList<BookingTimeRange>> GetScheduledTimeRangesAsync(long employeeId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    starts_at AS StartsAt,
                    ends_at AS EndsAt
                FROM bookings
                WHERE employee_id = @EmployeeId
                  AND status = 'scheduled'
                  AND starts_at < @To
                  AND ends_at > @From
                ORDER BY starts_at;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            IEnumerable<BookingTimeRange> ranges = await connection.QueryAsync<BookingTimeRange>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        EmployeeId = employeeId,
                        From = from.ToUniversalTime(),
                        To = to.ToUniversalTime()
                    },
                cancellationToken: cancellationToken));

            return ranges.ToList();
        }

        public async Task<PagedResult<AdminBookingListItem>> GetAdminBookingsAsync(
      AdminBookingSearchCriteria criteria,
      CancellationToken cancellationToken)
        {
            const string sql = """
        SELECT COUNT(*)
        FROM bookings AS b
        WHERE
            (
                @DateFrom IS NULL
                OR (
                    b.starts_at >= @DateFrom
                    AND b.starts_at < @DateTo
                )
            )
            AND (
                @From IS NULL
                OR b.starts_at >= @From
            )
            AND (
                @To IS NULL
                OR b.starts_at < @To
            )
            AND (
                @EmployeeId IS NULL
                OR b.employee_id = @EmployeeId
            )
            AND (
                @CustomerId IS NULL
                OR b.customer_id = @CustomerId
            )
            AND (
                @Status IS NULL
                OR b.status = @Status
            );

        SELECT
            b.id AS Id,
            b.customer_id AS CustomerId,

            cua.first_name AS CustomerFirstName,
            cua.last_name AS CustomerLastName,
            cua.email AS CustomerEmail,
            c.phone AS CustomerPhone,

            b.employee_id AS EmployeeId,
            eua.first_name AS EmployeeFirstName,
            eua.last_name AS EmployeeLastName,

            b.starts_at AS StartsAt,
            b.ends_at AS EndsAt,

            COALESCE(
                SUM(a.duration_minutes_at_booking), 0
            )::integer AS TotalDurationMinutes,

            COALESCE(
                SUM(a.price_at_booking), 0
            ) AS TotalPrice,

            b.status AS Status,
            b.notes AS Notes,
            b.created_at AS CreatedAt,
            b.updated_at AS UpdatedAt

        FROM
        (
            SELECT
                id,
                customer_id,
                employee_id,
                starts_at,
                ends_at,
                status,
                notes,
                created_at,
                updated_at
            FROM bookings
            WHERE
                (
                    @DateFrom IS NULL
                    OR (
                        starts_at >= @DateFrom
                        AND starts_at < @DateTo
                    )
                )
                AND (
                    @From IS NULL
                    OR starts_at >= @From
                )
                AND (
                    @To IS NULL
                    OR starts_at < @To
                )
                AND (
                    @EmployeeId IS NULL
                    OR employee_id = @EmployeeId
                )
                AND (
                    @CustomerId IS NULL
                    OR customer_id = @CustomerId
                )
                AND (
                    @Status IS NULL
                    OR status = @Status
                )
            ORDER BY
                starts_at ASC,
                id ASC
            LIMIT @PageSize
            OFFSET @Offset
        ) AS b

        INNER JOIN customers AS c
            ON c.id = b.customer_id

        INNER JOIN user_accounts AS cua
            ON cua.id = c.user_account_id

        INNER JOIN employees AS e
            ON e.id = b.employee_id

        INNER JOIN user_accounts AS eua
            ON eua.id = e.user_account_id

        LEFT JOIN appointments AS a
            ON a.booking_id = b.id

        GROUP BY
            b.id,
            b.customer_id,
            cua.first_name,
            cua.last_name,
            cua.email,
            c.phone,
            b.employee_id,
            eua.first_name,
            eua.last_name,
            b.starts_at,
            b.ends_at,
            b.status,
            b.notes,
            b.created_at,
            b.updated_at

        ORDER BY
            b.starts_at ASC,
            b.id ASC;
        """;

            DateTimeOffset? dateFromUtc = null;
            DateTimeOffset? dateToUtc = null;

            if (criteria.Date.HasValue)
            {
                DateTime localDateFrom =
                    criteria.Date.Value.ToDateTime(TimeOnly.MinValue);

                DateTime localDateTo =
                    criteria.Date.Value
                        .AddDays(1)
                        .ToDateTime(TimeOnly.MinValue);

                dateFromUtc =
                    new DateTimeOffset(
                        localDateFrom,
                        TimeZoneInfo.Local.GetUtcOffset(localDateFrom))
                    .ToUniversalTime();

                dateToUtc =
                    new DateTimeOffset(
                        localDateTo,
                        TimeZoneInfo.Local.GetUtcOffset(localDateTo))
                    .ToUniversalTime();
            }

            DateTimeOffset? fromUtc = null;

            if (criteria.From.HasValue)
            {
                DateTime localFrom =
                    criteria.From.Value.ToDateTime(TimeOnly.MinValue);

                fromUtc =
                    new DateTimeOffset(
                        localFrom,
                        TimeZoneInfo.Local.GetUtcOffset(localFrom))
                    .ToUniversalTime();
            }

            DateTimeOffset? toUtc = null;

            if (criteria.To.HasValue)
            {
                DateTime localTo =
                    criteria.To.Value
                        .AddDays(1)
                        .ToDateTime(TimeOnly.MinValue);

                toUtc =
                    new DateTimeOffset(
                        localTo,
                        TimeZoneInfo.Local.GetUtcOffset(localTo))
                    .ToUniversalTime();
            }

            string? status =
                criteria.Status?
                    .ToString()
                    .ToLowerInvariant();

            int offset =
                (criteria.Page - 1) *
                criteria.PageSize;

            var parameters = new
            {
                DateFrom = dateFromUtc,
                DateTo = dateToUtc,
                From = fromUtc,
                To = toUtc,
                EmployeeId = criteria.EmployeeId,
                CustomerId = criteria.CustomerId,
                Status = status,
                PageSize = criteria.PageSize,
                Offset = offset
            };

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(
                    cancellationToken);

            CommandDefinition command = new(
                sql,
                parameters,
                cancellationToken: cancellationToken);

            using GridReader result =
                await connection.QueryMultipleAsync(command);

            int totalCount =
                await result.ReadSingleAsync<int>();

            AdminBookingListItem[] items =
                (await result.ReadAsync<AdminBookingListItem>())
                .ToArray();

            return new PagedResult<AdminBookingListItem>
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<IReadOnlyList<BookableEmployeeResponse>> GetBookableEmployeesAsync(
            IReadOnlyCollection<long> serviceIds,
            CancellationToken cancellationToken)
                {
                    const string sql = """
                SELECT
                    e.id AS Id,
                    ua.first_name AS FirstName,
                    ua.last_name AS LastName
                FROM employees AS e
                INNER JOIN user_accounts AS ua
                    ON ua.id = e.user_account_id
                INNER JOIN employee_services AS es
                    ON es.employee_id = e.id
                WHERE
                    e.is_active = TRUE
                    AND ua.is_active = TRUE
                    AND es.is_active = TRUE
                    AND es.service_id = ANY(@ServiceIds)
                GROUP BY
                    e.id,
                    ua.first_name,
                    ua.last_name
                HAVING COUNT(DISTINCT es.service_id) = @ServiceCount
                ORDER BY
                    ua.first_name,
                    ua.last_name;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            IEnumerable<BookableEmployeeResponse> employees =
                await connection.QueryAsync<BookableEmployeeResponse>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            ServiceIds = serviceIds.ToArray(),
                            ServiceCount = serviceIds.Count
                        },
                        cancellationToken: cancellationToken));

            return employees.ToList();
        }

        private sealed class BookingReadRow
        {
            public long Id { get; set; }

            public long CustomerId { get; set; }

            public long EmployeeId { get; set; }

            public DateTimeOffset StartsAt { get; set; }

            public DateTimeOffset EndsAt { get; set; }

            public BookingStatus Status { get; set; } 

            public string? Notes { get; set; }

            public DateTimeOffset CreatedAt { get; set; }

            public DateTimeOffset? UpdatedAt { get; set; }

            public string EmployeeFirstName { get; set; } = null!;

            public string EmployeeLastName { get; set; } = null!;

            public DateTimeOffset? CompletedAt { get; set; }

            public CompletionSource? CompletionSource { get; set; }
        }
        private sealed class BookingListRow
        {
            public long Id { get; set; }

            public long CustomerId { get; set; }

            public long EmployeeId { get; set; }

            public DateTimeOffset StartsAt { get; set; }

            public DateTimeOffset EndsAt { get; set; }

            public BookingStatus Status { get; set; }

            public string? Notes { get; set; }

            public DateTimeOffset CreatedAt { get; set; }

            public DateTimeOffset? UpdatedAt { get; set; }

            public string EmployeeFirstName { get; set; } = null!;

            public string EmployeeLastName { get; set; } = null!;

            public int TotalDurationMinutes { get; set; }

            public decimal TotalPrice { get; set; }
        }
    }
}
