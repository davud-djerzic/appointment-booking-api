using AppointmentBooking.Api.Models;
using Dapper;
using Npgsql;

namespace AppointmentBooking.Api.Repositories.UserAccounts
{
    public sealed class UserAccountRepository(NpgsqlDataSource dataSource) : IUserAccountRepository
    {
        public async Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    id,
                    first_name AS FirstName,
                    last_name AS LastName,
                    email,
                    password_hash AS PasswordHash,
                    role,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM user_accounts
                WHERE email = @Email;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<UserAccount>(
                new CommandDefinition(
                    sql,
                    new { Email = email },
                    cancellationToken: cancellationToken));
        }

        public async Task<UserAccount?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    id,
                    first_name AS FirstName,
                    last_name AS LastName,
                    email,
                    password_hash AS PasswordHash,
                    role,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM user_accounts
                WHERE id = @Id;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<UserAccount>(
                new CommandDefinition(
                    sql,
                    new { Id = id },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> UpdatePasswordHashAsync(long userAccountId, string passwordHash, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE user_accounts
                SET
                    password_hash = @PasswordHash,
                    updated_at = NOW()
                WHERE id = @UserAccountId
                  AND is_active = TRUE;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            int rowsAffected = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        UserAccountId = userAccountId,
                        PasswordHash = passwordHash
                    },
                    cancellationToken: cancellationToken));

            return rowsAffected > 0;
        }
    }
}
