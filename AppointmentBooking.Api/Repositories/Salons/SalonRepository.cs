using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Models.Enums;
using Dapper;
using Npgsql;

namespace AppointmentBooking.Api.Repositories.Salons
{
    public sealed class SalonRepository(NpgsqlDataSource dataSource) : ISalonRepository
    {
        public async Task<SalonWithWorkingHours> CreateAsync(Salon salon, IReadOnlyCollection<SalonWorkingHours> workingHours, CancellationToken cancellationToken)
        {
            const string createSalonSql = """
                INSERT INTO salons
                (
                    name,
                    address,
                    city,
                    instagram_url,
                    facebook_url,
                    latitude,
                    longitude
                )
                VALUES
                (
                    @Name,
                    @Address,
                    @City,
                    @InstagramUrl,
                    @FacebookUrl,
                    @Latitude,
                    @Longitude
                )
                RETURNING
                    id,
                    name AS Name,
                    address AS Address,
                    city AS City,
                    instagram_url AS InstagramUrl,
                    facebook_url AS FacebookUrl,
                    latitude AS Latitude,
                    longitude AS Longitude,
                    is_active AS IsActive,
                    created_at AS CreatedAt,
                    updated_at AS UpdatedAt;
                """;

            const string createWorkingHoursSql = """
                INSERT INTO salon_working_hours
                (
                    salon_id,
                    day_of_week,
                    starts_at,
                    ends_at
                )
                VALUES
                (
                    @SalonId,
                    @DayOfWeek,
                    @StartsAt,
                    @EndsAt
                )
                RETURNING
                    id,
                    salon_id AS SalonId,
                    day_of_week AS DayOfWeek,
                    starts_at AS StartsAt,
                    ends_at AS EndsAt;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                Salon createdSalon = await connection.QuerySingleAsync<Salon>(
                    new CommandDefinition(
                        createSalonSql,
                        new
                        {
                            salon.Name,
                            salon.Address,
                            salon.City,
                            salon.InstagramUrl,
                            salon.FacebookUrl,
                            salon.Latitude,
                            salon.Longitude
                        },
                        transaction: transaction,
                        cancellationToken: cancellationToken));

                List<SalonWorkingHours> createdWorkingHours = new(workingHours.Count);

                foreach(SalonWorkingHours workingHour in workingHours)
                {
                    SalonWorkingHours created =
                        await connection.QuerySingleAsync<SalonWorkingHours>(
                            new CommandDefinition(
                                createWorkingHoursSql,
                                new
                                {
                                    SalonId = createdSalon.Id,
                                    DayOfWeek = (short)workingHour.DayOfWeek,
                                    StartsAt = workingHour.StartsAt.ToTimeSpan(),
                                    EndsAt = workingHour.EndsAt.ToTimeSpan()
                                },
                                transaction: transaction,
                                cancellationToken: cancellationToken));

                    createdWorkingHours.Add(created);
                }

                await transaction.CommitAsync(cancellationToken);

                return new SalonWithWorkingHours(createdSalon, createdWorkingHours);
            }
            catch (PostgresException ex)
                when (ex.SqlState == "23P01")
            {
                await transaction.RollbackAsync(cancellationToken);

                throw new ConflictException(
                    "The salon already has working hours that overlap this interval.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<SalonWithWorkingHours?> GetByIdAsync(long salonId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT 
                    s.id,
                    s.name AS Name,
                    s.address AS Address,
                    s.city AS City,
                    s.instagram_url AS InstagramUrl,
                    s.facebook_url AS FacebookUrl,
                    s.latitude AS Latitude,
                    s.longitude AS Longitude,
                    s.is_active AS IsActive,
                    s.created_at AS CreatedAt,
                    s.updated_at AS UpdatedAt,

                    sh.id AS WorkingHoursId,
                    sh.salon_id AS WorkingHoursSalonId,
                    sh.day_of_week AS WorkingHoursDayOfWeek,
                    sh.starts_at AS WorkingHoursStartsAt,
                    sh.ends_at AS WorkingHoursEndsAt
                FROM salons AS s
                LEFT JOIN salon_working_hours AS sh 
                    ON sh.salon_id = s.id
                WHERE s.id = @SalonId
                ORDER BY
                    sh.day_of_week,
                    sh.starts_at;
                """;

            await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

            Salon? salon = null;

            List<SalonWorkingHours> workingHours = [];

            await connection.QueryAsync<Salon, SalonWorkingHoursRow, Salon>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        SalonId = salonId
                    },
                    cancellationToken: cancellationToken),
                (salonRow, workingHoursRow) =>
                {
                    salon ??= salonRow;

                    if (workingHoursRow.WorkingHoursId.HasValue)
                    {
                        workingHours.Add(
                            new SalonWorkingHours
                            {
                                Id = workingHoursRow.WorkingHoursId.Value,
                                SalonId = workingHoursRow.WorkingHoursSalonId!.Value,
                                DayOfWeek = (WeekDay)
                                    workingHoursRow.WorkingHoursDayOfWeek!.Value,
                                StartsAt = workingHoursRow.WorkingHoursStartsAt!.Value,
                                EndsAt = workingHoursRow.WorkingHoursEndsAt!.Value
                            });
                    }

                    return salonRow;
                },
                splitOn: "WorkingHoursId");

            if (salon is null)
            {
                return null;
            }

            return new SalonWithWorkingHours(
                salon,
                workingHours);
        }

        private sealed class SalonWorkingHoursRow
        {
            public long? WorkingHoursId { get; init; }

            public long? WorkingHoursSalonId { get; init; }

            public short? WorkingHoursDayOfWeek { get; init; }

            public TimeOnly? WorkingHoursStartsAt { get; init; }

            public TimeOnly? WorkingHoursEndsAt { get; init; }
        }
    }
}
