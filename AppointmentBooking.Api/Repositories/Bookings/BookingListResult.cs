namespace AppointmentBooking.Api.Repositories.Bookings
{
    public sealed record BookingListResult(
        IReadOnlyList<BookingListItem> Items,
        int TotalCount);
}
