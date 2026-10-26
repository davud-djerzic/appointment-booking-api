using AppointmentBooking.Api.DTOs.Profile.Request;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using Dapper;
using Npgsql;

namespace AppointmentBooking.Api.Repositories.Customers
{
    public class CustomerRepository(NpgsqlDataSource dataSource) : ICustomerRepository
    {
        public async Task<Customer?> GetActiveByUserAccountIdAsync(long userAccountId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    c.id,
                    c.user_account_id AS UserAccountId,
                    c.phone AS Phone,
                    c.created_at AS CreatedAt,
                    c.updated_at AS UpdatedAt
                FROM customers c
                INNER JOIN user_accounts ua
                    ON ua.id = c.user_account_id
                WHERE c.user_account_id = @UserAccountId
                  AND ua.is_active = TRUE;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(
                    cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<Customer>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        UserAccountId = userAccountId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task UpdateProfileAsync(long userAccountId, UpdateMyProfileRequest request, CancellationToken cancellationToken)
        {
            await using NpgsqlConnection connection =
        await dataSource.OpenConnectionAsync(
            cancellationToken);

            await using NpgsqlTransaction transaction =
                await connection.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                if (request.FirstName is not null ||
                    request.LastName is not null ||
                    request.Email is not null)
                {
                    const string updateUserAccountSql = """
                        UPDATE user_accounts
                        SET
                            first_name = COALESCE(@FirstName, first_name),
                            last_name = COALESCE(@LastName, last_name),
                            email = COALESCE(@Email, email),
                            updated_at = NOW()
                        WHERE id = @UserAccountId;
                        """;

                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            updateUserAccountSql,
                            new
                            {
                                UserAccountId = userAccountId,
                                FirstName = request.FirstName,
                                LastName = request.LastName,
                                Email = request.Email
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));
                }

                if (request.Phone is not null)
                {
                    const string updateCustomerSql = """
                        UPDATE customers
                        SET
                            phone = @Phone,
                            updated_at = NOW()
                        WHERE user_account_id = @UserAccountId;
                        """;

                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            updateCustomerSql,
                            new
                            {
                                UserAccountId = userAccountId,
                                Phone = request.Phone
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (PostgresException ex)
                when (ex.SqlState == PostgresErrorCodes.UniqueViolation &&
                      ex.ConstraintName == "uq_user_accounts_email")
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new ConflictException(
                    "An account with this email already exists.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<bool> DeleteAccountAsync(
            long userAccountId,
            CancellationToken cancellationToken)
        {
            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            await using NpgsqlTransaction transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                const string getCustomerIdSql = """
                    SELECT c.id
                    FROM customers c
                    INNER JOIN user_accounts ua ON ua.id = c.user_account_id
                    WHERE c.user_account_id = @UserAccountId
                      AND ua.role = 'customer'
                      AND ua.is_active = TRUE
                    FOR UPDATE OF c, ua;
                    """;

                long? customerId =
                    await connection.QuerySingleOrDefaultAsync<long?>(
                        new CommandDefinition(
                            getCustomerIdSql,
                            new { UserAccountId = userAccountId },
                            transaction,
                            cancellationToken: cancellationToken));

                if (!customerId.HasValue)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return false;
                }

                const string cancelUpcomingAppointmentsSql = """
                    UPDATE appointments a
                    SET status = 'cancelled',
                        updated_at = NOW()
                    FROM bookings b
                    WHERE a.booking_id = b.id
                      AND b.customer_id = @CustomerId
                      AND b.status = 'scheduled'
                      AND b.starts_at > NOW()
                      AND a.status = 'scheduled';
                    """;

                const string anonymizeBookingsSql = """
                    UPDATE bookings
                    SET customer_id = NULL,
                        customer_first_name = 'Obrisan',
                        customer_last_name = 'korisnik',
                        notes = NULL,
                        status = CASE
                            WHEN status = 'scheduled' AND starts_at > NOW()
                                THEN 'cancelled'
                            ELSE status
                        END,
                        updated_at = NOW()
                    WHERE customer_id = @CustomerId;
                    """;

                const string deletePasswordResetCodesSql = """
                    DELETE FROM password_reset_codes
                    WHERE user_account_id = @UserAccountId;
                    """;

                const string unlinkRefreshTokensSql = """
                    UPDATE user_refresh_tokens
                    SET replaced_by_token_id = NULL
                    WHERE user_account_id = @UserAccountId;
                    """;

                const string deleteRefreshTokensSql = """
                    DELETE FROM user_refresh_tokens
                    WHERE user_account_id = @UserAccountId;
                    """;

                const string deleteCustomerSql = """
                    DELETE FROM customers
                    WHERE id = @CustomerId;
                    """;

                const string deleteUserAccountSql = """
                    DELETE FROM user_accounts
                    WHERE id = @UserAccountId
                      AND role = 'customer';
                    """;

                var parameters = new
                {
                    UserAccountId = userAccountId,
                    CustomerId = customerId.Value
                };

                await connection.ExecuteAsync(new CommandDefinition(
                    cancelUpcomingAppointmentsSql,
                    parameters,
                    transaction,
                    cancellationToken: cancellationToken));
                await connection.ExecuteAsync(new CommandDefinition(
                    anonymizeBookingsSql,
                    parameters,
                    transaction,
                    cancellationToken: cancellationToken));
                await connection.ExecuteAsync(new CommandDefinition(
                    deletePasswordResetCodesSql,
                    parameters,
                    transaction,
                    cancellationToken: cancellationToken));
                await connection.ExecuteAsync(new CommandDefinition(
                    unlinkRefreshTokensSql,
                    parameters,
                    transaction,
                    cancellationToken: cancellationToken));
                await connection.ExecuteAsync(new CommandDefinition(
                    deleteRefreshTokensSql,
                    parameters,
                    transaction,
                    cancellationToken: cancellationToken));
                await connection.ExecuteAsync(new CommandDefinition(
                    deleteCustomerSql,
                    parameters,
                    transaction,
                    cancellationToken: cancellationToken));

                int deletedAccounts = await connection.ExecuteAsync(
                    new CommandDefinition(
                        deleteUserAccountSql,
                        parameters,
                        transaction,
                        cancellationToken: cancellationToken));

                if (deletedAccounts != 1)
                {
                    throw new InvalidOperationException(
                        "The customer account could not be deleted.");
                }

                await transaction.CommitAsync(cancellationToken);
                return true;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    
    }
}
