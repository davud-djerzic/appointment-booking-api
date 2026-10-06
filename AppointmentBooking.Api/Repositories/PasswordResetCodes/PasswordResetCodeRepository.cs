using AppointmentBooking.Api.Models;
using Dapper;
using Npgsql;

namespace AppointmentBooking.Api.Repositories.PasswordResetCodes
{
    public sealed class PasswordResetCodeRepository(NpgsqlDataSource dataSource) : IPasswordResetCodeRepository
    {
        public async Task CreateAsync(PasswordResetCode passwordResetCode, CancellationToken cancellationToken)
        {
            const string sql = """
            INSERT INTO password_reset_codes
            (
                user_account_id,
                code_hash,
                expires_at,
                attempts,
                verified_at,
                used_at,
                created_at
            )
            VALUES
            (
                @UserAccountId,
                @CodeHash,
                @ExpiresAt,
                @Attempts,
                @VerifiedAt,
                @UsedAt,
                @CreatedAt
            );
            """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        passwordResetCode.UserAccountId,
                        passwordResetCode.CodeHash,
                        passwordResetCode.ExpiresAt,
                        passwordResetCode.Attempts,
                        passwordResetCode.VerifiedAt,
                        passwordResetCode.UsedAt,
                        passwordResetCode.CreatedAt
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<PasswordResetCode?> GetLatestActiveAsync(long userAccountId, CancellationToken cancellationToken)
        {
            const string sql = """
            SELECT
                id,
                user_account_id AS UserAccountId,
                code_hash AS CodeHash,
                expires_at AS ExpiresAt,
                attempts AS Attempts,
                verified_at AS VerifiedAt,
                used_at AS UsedAt,
                created_at AS CreatedAt
            FROM password_reset_codes
            WHERE user_account_id = @UserAccountId
              AND used_at IS NULL
              AND expires_at > NOW()
              AND attempts < 5
            ORDER BY created_at DESC
            LIMIT 1;
            """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<PasswordResetCode>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        UserAccountId = userAccountId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task<bool> IncrementAttemptsAsync(long id,CancellationToken cancellationToken)
        {
            const string sql = """
            UPDATE password_reset_codes
            SET attempts = attempts + 1
            WHERE id = @Id
              AND used_at IS NULL
              AND expires_at > NOW()
              AND attempts < 5;
            """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            int affectedRows = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Id = id
                    },
                    cancellationToken: cancellationToken));

            return affectedRows > 0;
        }

        public async Task<bool> MarkVerifiedAsync(
            long id,
            CancellationToken cancellationToken)
        {
            const string sql = """
            UPDATE password_reset_codes
            SET verified_at = NOW()
            WHERE id = @Id
              AND used_at IS NULL
              AND verified_at IS NULL
              AND expires_at > NOW()
              AND attempts < 5;
            """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            int affectedRows = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Id = id
                    },
                    cancellationToken: cancellationToken));

            return affectedRows > 0;
        }

        public async Task<bool> MarkUsedAsync(long id,CancellationToken cancellationToken)
        {
            const string sql = """
            UPDATE password_reset_codes
            SET used_at = NOW()
            WHERE id = @Id
              AND verified_at IS NOT NULL
              AND used_at IS NULL;
            """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            int affectedRows = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Id = id
                    },
                    cancellationToken: cancellationToken));

            return affectedRows > 0;
        }

        public async Task InvalidateActiveAsync(
            long userAccountId,
            CancellationToken cancellationToken)
        {
            const string sql = """
            UPDATE password_reset_codes
            SET used_at = NOW()
            WHERE user_account_id = @UserAccountId
              AND used_at IS NULL
              AND expires_at > NOW();
            """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            await connection.ExecuteAsync(
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
