namespace AppointmentBooking.Api.Repositories.Appointments
{
    public sealed record ConfirmAppointmentData(long AppointmentId, string CustomerFirstName, string CustomerLastName, string? CustomerEmail, string? CustomerPhone, string? Notes);
}
