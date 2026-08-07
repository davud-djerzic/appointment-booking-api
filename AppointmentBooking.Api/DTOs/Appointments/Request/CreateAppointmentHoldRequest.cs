namespace AppointmentBooking.Api.DTOs.Appointments.Request
{
    public sealed record CreateAppointmentHoldRequest(long EmployeeId, long ServiceId, DateTimeOffset StartsAt);
}
