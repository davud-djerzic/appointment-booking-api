using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Bookings
{
    public interface IBookingRepository
    {
        Task<bool> HasScheduledOverlapAsync(long employeeId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken);

        Task<BookingCreationResult> CreateAsync(Booking booking, IReadOnlyList<Appointment> appointments, CancellationToken cancellationToken);

        Task<BookingReadResult?> GetByIdAsync(long bookingId, CancellationToken cancellationToken);

        Task<PagedResult<BookingListItem>> GetCustomerBookingsAsync(long customerId, BookingSearchCriteria criteria, CancellationToken cancellationToken);

        Task<bool> CancelAsync(long bookingId, CancellationToken cancellationToken);

        Task<bool> CompleteAsync(long bookingId, CancellationToken cancellationToken);

        Task<int> CompleteExpiredAutomaticallyAsync(DateTimeOffset completedAt, CancellationToken cancellationToken);

        Task<PagedResult<EmployeeBookingListItem>> GetEmployeeBookingsAsync(long employeeId, BookingSearchCriteria criteria, CancellationToken cancellationToken);

        Task<IReadOnlyList<BookingTimeRange>> GetScheduledTimeRangesAsync(long employeeId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

        Task<PagedResult<AdminBookingListItem>> GetAdminBookingsAsync(AdminBookingSearchCriteria criteria, CancellationToken cancellationToken);
    }
}
