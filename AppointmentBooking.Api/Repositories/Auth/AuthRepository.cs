using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using Dapper;
using Npgsql;

namespace AppointmentBooking.Api.Repositories.Auth
{
    public sealed class AuthRepository(NpgsqlDataSource dataSource) : IAuthRepository
    {
        public async Task<UserAccount> CreateCustomerAccountAsync(UserAccount userAccount, Customer customer, Guid familyId, string refreshTokenHash, DateTimeOffset refreshTokenExpiresAt, CancellationToken cancellationToken)
        {
            const string createUserAccountSql = """
                INSERT INTO user_accounts
                (
                    first_name,
                    last_name,
                    email,
                    password_hash,
                    role,
                    is_active
                )
                VALUES
                (
                    @FirstName,
                    @LastName,
                    @Email,
                    @PasswordHash,
                    @Role,
                    @IsActive
                )
                RETURNING
                    id,
                    first_name AS FirstName,
                    last_name AS LastName,
                    email,
                    password_hash AS PasswordHash,
                    role,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            const string createCustomerSql = """
                INSERT INTO customers
                (
                    user_account_id,
                    phone
                )
                VALUES
                (
                    @UserAccountId,
                    @Phone
                );
                """;

            const string createRefreshTokenSql = """
                INSERT INTO user_refresh_tokens
                (
                    user_account_id,
                    family_id,
                    token_hash,
                    expires_at
                )
                VALUES
                (
                    @UserAccountId,
                    @FamilyId,
                    @TokenHash,
                    @ExpiresAt
                );
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            await using NpgsqlTransaction transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                UserAccount createdUserAccount =
                    await connection.QuerySingleAsync<UserAccount>(
                        new CommandDefinition(
                            createUserAccountSql,
                            new
                            {
                                userAccount.FirstName,
                                userAccount.LastName,
                                userAccount.Email,
                                userAccount.PasswordHash,
                                Role = userAccount.Role
                                    .ToString()
                                    .ToLowerInvariant(),
                                userAccount.IsActive
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        createCustomerSql,
                        new
                        {
                            UserAccountId = createdUserAccount.Id,
                            customer.Phone
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        createRefreshTokenSql,
                        new
                        {
                            UserAccountId = createdUserAccount.Id,
                            FamilyId = familyId,
                            TokenHash = refreshTokenHash,
                            ExpiresAt = refreshTokenExpiresAt
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                return createdUserAccount;
            }
            catch (PostgresException ex)
                when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                await transaction.RollbackAsync(cancellationToken);

                if (ex.ConstraintName == "uq_user_accounts_email")
                {
                    throw new ConflictException(
                        "An account with this email already exists.");
                }

                throw;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task CreateRefreshTokenAsync(long userAccountId, Guid familyId, string refreshTokenHash, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO user_refresh_tokens
                (
                    user_account_id,
                    family_id,
                    token_hash,
                    expires_at
                )
                VALUES
                (
                    @UserAccountId,
                    @FamilyId,
                    @TokenHash,
                    @ExpiresAt
                );
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new(
                sql,
                new
                {
                    UserAccountId = userAccountId,
                    FamilyId = familyId,
                    TokenHash = refreshTokenHash,
                    ExpiresAt = expiresAt
                },
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

        public async Task<UserRefreshToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    id,
                    user_account_id AS UserAccountId,
                    token_hash AS TokenHash,
                    family_id AS FamilyId,
                    expires_at AS ExpiresAt,
                    created_at AS CreatedAt,
                    revoked_at AS RevokedAt,
                    replaced_by_token_id AS ReplacedByTokenId
                FROM user_refresh_tokens
                WHERE token_hash = @TokenHash;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new(
                sql,
                new { TokenHash = tokenHash },
                cancellationToken: cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<UserRefreshToken>(command);
        }

        public async Task<RefreshTokenRotationResult?> RotateRefreshTokenAsync(long currentRefreshTokenId, string newRefreshTokenHash, DateTimeOffset newRefreshTokenExpiresAt, CancellationToken cancellationToken)
        {
            const string revokeCurrentTokenSql = """
                UPDATE user_refresh_tokens
                SET
                    revoked_at = NOW()
                WHERE id = @CurrentRefreshTokenId
                  AND revoked_at IS NULL
                RETURNING user_account_id AS UserAccountId,
                          family_id AS FamilyId;
                """;

            const string createNewTokenSql = """
                INSERT INTO user_refresh_tokens
                (
                    user_account_id,
                    family_id,
                    token_hash,
                    expires_at
                )
                VALUES
                (
                    @UserAccountId,
                    @FamilyId,
                    @NewRefreshTokenHash,
                    @NewRefreshTokenExpiresAt
                )
                RETURNING id;
                """;

            const string linkReplacedTokenSql = """
                UPDATE user_refresh_tokens
                SET
                    replaced_by_token_id = @NewRefreshTokenId
                WHERE id = @CurrentRefreshTokenId;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(cancellationToken);

            await using NpgsqlTransaction transaction =
                await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                CurrentRefreshTokenData? currentToken = await connection.QuerySingleOrDefaultAsync<CurrentRefreshTokenData>(
               new CommandDefinition(
                   revokeCurrentTokenSql,
                   new
                   {
                       CurrentRefreshTokenId = currentRefreshTokenId
                   },
                   transaction: transaction,
                   cancellationToken: cancellationToken));

                if (currentToken is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                long newRefreshTokenId =
                    await connection.QuerySingleAsync<long>(
                        new CommandDefinition(
                            createNewTokenSql,
                            new
                            {
                                UserAccountId = currentToken.UserAccountId,
                                FamilyId = currentToken.FamilyId,
                                NewRefreshTokenHash = newRefreshTokenHash,
                                NewRefreshTokenExpiresAt = newRefreshTokenExpiresAt
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        linkReplacedTokenSql,
                        new
                        {
                            CurrentRefreshTokenId = currentRefreshTokenId,
                            NewRefreshTokenId = newRefreshTokenId
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                return new RefreshTokenRotationResult(currentToken.UserAccountId, currentToken.FamilyId, newRefreshTokenId);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private sealed record CurrentRefreshTokenData(
            long UserAccountId,
            Guid FamilyId);

        public async Task RevokeRefreshTokenAsync(long refreshTokenId, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE user_refresh_tokens
                SET revoked_at = NOW()
                WHERE id = @RefreshTokenId
                AND revoked_at IS NULL;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        RefreshTokenId = refreshTokenId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task RevokeRefreshTokenFamilyAsync(Guid familyId, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE user_refresh_tokens
                SET revoked_at = NOW()
                WHERE family_id = @FamilyId
                  AND revoked_at IS NULL;
                """;

            await using NpgsqlConnection connection =
                await dataSource.OpenConnectionAsync(
                    cancellationToken);

            await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    FamilyId = familyId
                },
                cancellationToken: cancellationToken));

        }
    }
}
