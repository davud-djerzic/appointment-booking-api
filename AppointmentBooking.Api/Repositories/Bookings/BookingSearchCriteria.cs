using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.Repositories.Bookings
{
    public sealed class BookingSearchCriteria
    {
        public BookingStatus? Status { get; init; }

        public DateTimeOffset? From { get; init; }

        public DateTimeOffset? To { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }
    }
}
