namespace AppointmentBooking.Api.DTOs.Appointments.Request
{
    public sealed record ConfirmHoldAppointment(string CustomerFirstName, string CustomerLastName, string? CustomerEmail, string? CustomerPhone, string? Notes);

}
