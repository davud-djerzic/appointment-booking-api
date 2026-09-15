using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Bookings
{
    public sealed record BookingCreationResult(Booking Booking, IReadOnlyList<Appointment> Appointments);
}
