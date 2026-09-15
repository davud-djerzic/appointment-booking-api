using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Extension;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Models.Enums;
using Dapper;
using Npgsql;

namespace AppointmentBooking.Api.Repositories.Appointments
{
    public class AppointmentRepository(NpgsqlDataSource dataSource) : IAppointmentRepository
    {
       /* public async Task<Appointment> CreateHoldAsync(CreateAppointmentHoldData data, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO appointments
                (
                    employee_id,
                    service_id,
                    starts_at,
                    ends_at,
                    status,
                    hold_token,
                    hold_expires_at
                )
                VALUES
                (
                    @EmployeeId,
                    @ServiceId,
                    @StartsAt,
                    @EndsAt,
                    @Status,
                    @HoldToken,
                    @HoldExpiresAt
                )
                RETURNING
                    id,
                    employee_id AS EmployeeId,
                    service_id AS ServiceId,
                    customer_first_name AS CustomerFirstName,
                    customer_last_name AS CustomerLastName,
                    customer_email AS CustomerEmail,
                    customer_phone AS CustomerPhone,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt,
                    status AS Status,
                    hold_token AS HoldToken,
                    hold_expires_at AS HoldExpiresAt,
                    notes AS Notes,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            try
            {
                CommandDefinition command = new CommandDefinition(
                    sql,
                    new
                    {
                        data.EmployeeId,
                        data.ServiceId,
                        data.StartsAt,
                        data.EndsAt,
                        Status = AppointmentStatus.Held.ToDatabaseValue(),
                        data.HoldToken,
                        data.HoldExpiresAt
                    },
                    cancellationToken: cancellationToken);

                return await connection.QuerySingleAsync<Appointment>(command);
            } catch (PostgresException ex)
                when (ex.SqlState == "23P01")
            {
                throw new AppointmentSlotUnavailableException(ex);
            }
        }*/

        /*public async Task<Appointment?> GetByHoldTokenAsync(Guid holdToken, CancellationToken cancellationToken)
        {
            string sql = """
                SELECT 
                    id,
                    employee_id AS EmployeeId,
                    service_id AS ServiceId,
                    customer_first_name AS CustomerFirstName,
                    customer_last_name AS CustomerLastName,
                    customer_email AS CustomerEmail,
                    customer_phone AS CustomerPhone,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt,
                    status AS Status,
                    hold_token AS HoldToken,
                    hold_expires_at AS HoldExpiresAt,
                    notes AS Notes,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM appointments
                WHERE hold_token = @HoldToken;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                new { HoldToken = holdToken },
                cancellationToken: cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<Appointment>(command);
        }*/

        public async Task<Appointment?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    id,
                    booking_id AS BookingId,
                    service_id AS ServiceId,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt,
                    status AS Status,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM appointments
                WHERE id = @Id;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new(
                sql,
                new { Id = id },
                cancellationToken: cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<Appointment>(command);
        }
        /*public async Task<Appointment> ConfirmHoldAsync(ConfirmAppointmentData data, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE appointments
                SET 
                    customer_first_name = @CustomerFirstName,
                    customer_last_name = @CustomerLastName,
                    customer_email = @CustomerEmail,
                    customer_phone = @CustomerPhone,
                    status = @Status,
                    hold_token = NULL,
                    hold_expires_at = NULL,
                    notes = @Notes,
                    updated_at = NOW()
                WHERE id = @AppointmentId
                RETURNING
                    id,
                    employee_id AS EmployeeId,
                    service_id AS ServiceId,
                    customer_first_name AS CustomerFirstName,
                    customer_last_name AS CustomerLastName,
                    customer_email AS CustomerEmail,
                    customer_phone AS CustomerPhone,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt,
                    status AS Status,
                    hold_token AS HoldToken,
                    hold_expires_at AS HoldExpiresAt,
                    notes AS Notes,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                new
                {
                    data.AppointmentId,
                    data.CustomerFirstName,
                    data.CustomerLastName,
                    data.CustomerEmail,
                    data.CustomerPhone,
                    Status = AppointmentStatus.Scheduled.ToDatabaseValue(),
                    data.Notes
                },
                cancellationToken: cancellationToken);

            return await connection.QuerySingleAsync<Appointment>(command);
        }*/

        /*public async Task<bool> DeleteHeldAsync(long id, CancellationToken cancellationToken)
        {
            string sql = """
                DELETE FROM appointments
                WHERE id = @Id;
                """;
            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                new { Id = id },
                cancellationToken: cancellationToken);

            int affectedRows = await connection.ExecuteAsync(command);
            return affectedRows > 0;
        }*/

       /* public async Task<bool> CancelAsync(long appointmentId, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE appointments
                SET 
                    status = @Status,
                    updated_at = NOW()
                WHERE id = @AppointmentId; 
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                new
                {
                    AppointmentId = appointmentId,
                    Status = AppointmentStatus.Cancelled.ToDatabaseValue()
                },
                cancellationToken: cancellationToken);

            int affectedRows = await connection.ExecuteAsync(command);
            return affectedRows > 0;
        }*/

       /* public async Task<bool> CompleteAsync(long appointmentId, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE appointments
                SET 
                    status = @Status,
                    updated_at = NOW()
                WHERE id = @AppointmentId; 
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                new
                {
                    AppointmentId = appointmentId,
                    Status = AppointmentStatus.Completed.ToDatabaseValue()
                },
                cancellationToken: cancellationToken);

            int affectedRows = await connection.ExecuteAsync(command);
            return affectedRows > 0;
        }*/

      /*  public async Task<int> DeleteExpiredHoldsAsync(CancellationToken cancellationToken)
        {
            const string sql = """
                DELETE FROM appointments
                WHERE status = @Status AND hold_expires_at <= NOW();
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                new
                {
                    Status = AppointmentStatus.Held.ToDatabaseValue(),
                },
                cancellationToken: cancellationToken);

            return await connection.ExecuteAsync(command);
        }

        public async Task<int> CompleteExpiredAppointmentsAsync(CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE appointments
                SET status = @Status,
                    updated_at = NOW()
                WHERE status = @ScheduledStatus AND ends_at <= NOW();
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                new
                {
                    Status = AppointmentStatus.Completed.ToDatabaseValue(),
                    ScheduledStatus = AppointmentStatus.Scheduled.ToDatabaseValue()
                },
                cancellationToken: cancellationToken);

            return await connection.ExecuteAsync(command);
        }

        public async Task<PagedResult<AppointmentListItem>> GetAllAsync(AppointmentSearchCriteria criteria, CancellationToken cancellationToken)
        {
            List<string> conditions = [];

            if (criteria.EmployeeId.HasValue) conditions.Add("b.employee_id = @EmployeeId");
            

            if (criteria.ServiceId.HasValue) conditions.Add("a.service_id = @ServiceId");
            

            if (criteria.Status.HasValue) conditions.Add("a.status = @Status");
            

            if (criteria.Date.HasValue) conditions.Add("a.starts_at::date = @Date");
           

            string whereClause = conditions.Count > 0
                    ? $"WHERE {string.Join(" AND ", conditions)}"
                    : string.Empty;

            const string sqlTemplate = """
                SELECT COUNT(*)
                FROM appointments a
                INNER JOIN bookings b
                    ON a.booking_id = b.id
                INNER JOIN customers c
                    ON b.customer_id = c.id
                INNER JOIN user_accounts customer_account
                    ON c.user_account_id = customer_account.id
                INNER JOIN employees e
                    ON b.employee_id = e.id
                INNER JOIN user_accounts employee_account
                    ON e.user_account_id = employee_account.id
                INNER JOIN services s
                    ON a.service_id = s.id
                {0};

                SELECT
                    a.id AS Id,
                    a.booking_id AS BookingId,

                    b.customer_id AS CustomerId,
                    customer_account.first_name AS CustomerFirstName,
                    customer_account.last_name AS CustomerLastName,
                    customer_account.email AS CustomerEmail,
                    c.phone AS CustomerPhone,

                    b.employee_id AS EmployeeId,
                    employee_account.first_name AS EmployeeFirstName,
                    employee_account.last_name AS EmployeeLastName,

                    a.service_id AS ServiceId,
                    s.name AS ServiceName,

                    a.starts_at AS StartsAt,
                    a.ends_at AS EndsAt,
                    a.status AS Status,
                    b.notes AS Notes

                FROM appointments a

                INNER JOIN bookings b
                    ON a.booking_id = b.id

                INNER JOIN customers c
                    ON b.customer_id = c.id

                INNER JOIN user_accounts customer_account
                    ON c.user_account_id = customer_account.id

                INNER JOIN employees e
                    ON b.employee_id = e.id

                INNER JOIN user_accounts employee_account
                    ON e.user_account_id = employee_account.id

                INNER JOIN services s
                    ON a.service_id = s.id

                {0}

                ORDER BY a.starts_at
                LIMIT @PageSize
                OFFSET @Offset;
                """;

            string sql = string.Format(
                sqlTemplate,
                whereClause);

            var parameters = new
            {
                criteria.EmployeeId,
                criteria.ServiceId,
                Status = criteria.Status?.ToDatabaseValue(),
                Date = criteria.Date,
                criteria.PageSize,
                Offset = (criteria.Page - 1) * criteria.PageSize
            };

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new(
                sql,
                parameters,
                cancellationToken: cancellationToken);

            using var result =
                await connection.QueryMultipleAsync(command);

            int totalCount =
                await result.ReadSingleAsync<int>();

            AppointmentListItem[] appointments =
                (await result.ReadAsync<AppointmentListItem>())
                .ToArray();

            return new PagedResult<AppointmentListItem>
            {
                Items = appointments,
                TotalCount = totalCount
            };
        }/*/
    }
}
