namespace AppointmentBooking.Api.Repositories.Appointments
{
    public sealed record CreateAppointmentHoldData(long EmployeeId, long ServiceId, DateTimeOffset StartsAt, DateTimeOffset EndsAt, Guid HoldToken, DateTimeOffset HoldExpiresAt);

}
