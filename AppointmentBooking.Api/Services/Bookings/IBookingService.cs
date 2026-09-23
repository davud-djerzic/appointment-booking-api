using AppointmentBooking.Api.DTOs.BookingHolds.Response;
using AppointmentBooking.Api.DTOs.Bookings.Request;
using AppointmentBooking.Api.DTOs.Bookings.Response;
using AppointmentBooking.Api.DTOs.Common;

namespace AppointmentBooking.Api.Services.Bookings
{
    public interface IBookingService
    {
        Task<BookingHoldResponse> CreateHoldAsync(CreateBookingRequest request, CancellationToken cancellationToken);

        Task<BookingResponse> ConfirmAsync(Guid holdToken, CancellationToken cancellationToken);

        Task<BookingDetailsResponse> GetByIdAsync(long bookingId, CancellationToken cancellationToken);

        Task<PagedResponse<BookingSummaryResponse>> GetMyBookingsAsync(GetBookingsQuery query, CancellationToken cancellationToken);

        Task CancelHoldAsync(Guid holdToken, CancellationToken cancellationToken);

        Task CancelBookingAsync(long bookingId, CancellationToken cancellationToken);

        Task CompleteBookingAsync(long bookingId, CancellationToken cancellationToken);

        Task<PagedResponse<EmployeeBookingSummaryResponse>> GetEmployeeBookingsAsync(GetBookingsQuery query, CancellationToken cancellationToken);

        Task<BookingAvailabilityResponse> GetAvailabilityAsync(GetBookingAvailabilityQuery query, CancellationToken cancellationToken);

        Task<PagedResponse<AdminBookingSummaryResponse>> GetAdminBookingsAsync(GetAdminBookingsQuery query, CancellationToken cancellationToken);

        Task<QuickAvailabilityResponse> GetQuickAvailabilityAsync(GetQuickAvailabilityQuery query, CancellationToken cancellationToken);

        Task<IReadOnlyList<BookableEmployeeResponse>> GetBookableEmployeesAsync(IReadOnlyCollection<long> serviceIds, CancellationToken cancellationToken);
    }
}
