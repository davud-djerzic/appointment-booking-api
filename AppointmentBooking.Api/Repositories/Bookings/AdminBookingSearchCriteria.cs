using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.Repositories.Bookings
{
    public sealed class AdminBookingSearchCriteria
    {
        public DateOnly? Date { get; init; }

        public DateOnly? From { get; init; }

        public DateOnly? To { get; init; }

        public long? EmployeeId { get; init; }

        public long? CustomerId { get; init; }

        public BookingStatus? Status { get; init; }

        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 100;
    }
}
