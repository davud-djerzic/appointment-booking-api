using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Bookings
{
    public sealed record BookingReadResult(
        Booking Booking,
        string EmployeeFirstName,
        string EmployeeLastName,
        IReadOnlyList<Appointment> Appointments);
}
