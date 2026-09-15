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
    }
}
