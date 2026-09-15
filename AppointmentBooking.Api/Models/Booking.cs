using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.Models
{
    public sealed class Booking
    {
        public long Id { get; init; }

        public long CustomerId { get; init; }

        public long EmployeeId { get; init; }

        public DateTimeOffset StartsAt { get; init; }

        public DateTimeOffset EndsAt { get; init; }

        public BookingStatus Status { get; init; }

        public string? Notes { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }

        public DateTimeOffset? CompletedAt { get; set; }

        public CompletionSource? CompletionSource { get; set; }
    }
}
