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
    
    }
}
