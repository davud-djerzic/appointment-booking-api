using Dapper;
using Npgsql;

namespace AppointmentBooking.Api.Repositories
{
    public sealed class DatabaseHealthRepository(NpgsqlDataSource dataSource) : IDatabaseHealthRepository 
    {
        public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
        {
            const string sql = "SELECT 1";

            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
                
                var commnand = new CommandDefinition(sql, cancellationToken: cancellationToken);

                var result = await connection.ExecuteScalarAsync<int>(commnand);

                return result == 1;
            }
            catch
            {
                return false;
            }
        }
    }
}
