using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.BookingHolds
{
    public interface IBookingHoldRepository
    {
        Task<bool> TryCreateAsync(BookingHold hold, TimeSpan ttl, CancellationToken cancellationToken);

        Task<BookingHold?> GetAsync(Guid holdToken, CancellationToken cancellationToken);

        Task DeleteAsync(BookingHold hold, CancellationToken cancellationToken);

        Task<IReadOnlyList<BookingHold>> GetActiveForEmployeeAsync(long employeeId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
    }
}
