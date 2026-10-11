namespace AppointmentBooking.Api.DTOs.Bookings.Response;

public sealed record BookingCalendarDaysResponse(
    long EmployeeId,
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DateOnly> WorkingDates);
