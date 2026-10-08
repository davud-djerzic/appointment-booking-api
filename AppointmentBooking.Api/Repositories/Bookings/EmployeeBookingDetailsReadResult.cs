using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Bookings
{
    public sealed record EmployeeBookingDetailsReadResult(
        Booking Booking,
        long? CustomerId,
        string CustomerFirstName,
        string CustomerLastName,
        string? CustomerEmail,
        string? CustomerPhone,
        IReadOnlyList<Appointment> Appointments);
}
