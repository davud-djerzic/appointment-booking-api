using AppointmentBooking.Api.DTOs.EmployeeServices.Responses;
using AppointmentBooking.Api.Models;
using Dapper;
using Npgsql;

namespace AppointmentBooking.Api.Repositories.EmployeeServices
{
    public sealed class EmployeeServiceRepository(NpgsqlDataSource dataSource) : IEmployeeServiceRepository
    {
        public async Task<EmployeeServiceAssignemnt?> GetAsync(long employeeId, long serviceId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT 
                    employee_id AS EmployeeId,
                    service_id AS ServiceId,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM employee_services
                WHERE employee_id = @EmployeeId 
                    AND service_id = @ServiceId;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                new
                {
                    EmployeeId = employeeId,
                    ServiceId = serviceId
                },
                cancellationToken: cancellationToken);

            return await connection.QuerySingleOrDefaultAsync<EmployeeServiceAssignemnt>(command);
        }
       

        public async Task<EmployeeServiceAssignemnt> AssignOrReactivateAsync(long employeeId, long serviceId, CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO employee_services
                (
                    employee_id,
                    service_id
                )
                VALUES
                (
                    @EmployeeId,
                    @ServiceId
                )
                ON CONFLICT (employee_id, service_id)
                DO UPDATE
                SET
                    updated_at = 
                        CASE
                            WHEN employee_services.is_active = FALSE THEN NOW()
                            ELSE employee_services.updated_at
                        END,
                    is_active = TRUE
                RETURNING
                    employee_id AS EmployeeId,
                    service_id AS ServiceId,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                new
                {
                    EmployeeId = employeeId,
                    ServiceId = serviceId,
                },
                cancellationToken: cancellationToken
                );

            return await connection.QuerySingleAsync<EmployeeServiceAssignemnt>(command);
        }

        public async Task<bool> DeactivateAsync(long employeeId, long serviceId, CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE employee_services
                SET
                    updated_at = 
                        CASE
                            WHEN is_active = TRUE THEN NOW()
                            ELSE updated_at
                        END,
                    is_active = FALSE
                WHERE employee_id = @EmployeeId 
                    AND service_id = @ServiceId;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            CommandDefinition command = new CommandDefinition(
                sql,
                new
                {
                    EmployeeId = employeeId,
                    ServiceId = serviceId
                },
                cancellationToken: cancellationToken);

            int affectedRows = await connection.ExecuteAsync(command);

            return affectedRows > 0;
        }

        public async Task<IEnumerable<EmployeeServiceResponse>> GetEmployeeServicesAsync(long employeeId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT 
                    s.id AS Id,
                    s.name AS Name,
                    s.description AS Description,
                    s.price AS Price,
                    s.duration_minutes AS DurationMinutes
                FROM employee_services AS es
                INNER JOIN services s
                    ON s.id = es.service_id
                WHERE 
                    es.employee_id = @EmployeeId 
                    AND es.is_active = TRUE
                    AND s.is_active = TRUE
                ORDER BY s.name;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            return await connection.QueryAsync<EmployeeServiceResponse>(new CommandDefinition(
                sql,
                new { EmployeeId = employeeId },
                cancellationToken: cancellationToken));
        }

        public async Task<IEnumerable<ServiceEmployeeResponse>> GetServiceEmployeesAsync(long serviceId, CancellationToken cancellationToken)
        {

            const string sql = """
                SELECT 
                    e.id AS Id,
                    e.first_name AS FirstName,
                    e.last_name AS LastName,
                    e.email AS Email
                FROM employee_services AS es
                INNER JOIN employees e
                    ON e.id = es.employee_id
                WHERE 
                    es.service_id = @ServiceId 
                    AND es.is_active = TRUE
                    AND e.is_active = TRUE
                ORDER BY e.first_name, e.last_name;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            return await connection.QueryAsync<ServiceEmployeeResponse>(new CommandDefinition(
                sql,
                new { ServiceId = serviceId },
                cancellationToken: cancellationToken));
        }

        public async Task<EmployeeServiceAssignemnt> GetServiceEmployeesAssignmentAsync(long employeeId, long serviceId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT 
                    employee_id AS EmployeeId,
                    service_id AS ServiceId,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt
                FROM employee_services
                WHERE employee_id = @EmployeeId 
                    AND service_id = @ServiceId;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
            return await connection.QuerySingleAsync<EmployeeServiceAssignemnt>(new CommandDefinition(
                sql,
                new { EmployeeId = employeeId, ServiceId = serviceId },
                cancellationToken: cancellationToken));
        }

    }
}
