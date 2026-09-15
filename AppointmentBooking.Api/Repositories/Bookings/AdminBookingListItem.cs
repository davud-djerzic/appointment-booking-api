using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.Repositories.Bookings
{
    public sealed class AdminBookingListItem
    {
        public long Id { get; set; }

        public long CustomerId { get; set; }

        public string CustomerFirstName { get; set; } = null!;

        public string CustomerLastName { get; set; } = null!;

        public string CustomerEmail { get; set; } = null!;

        public string CustomerPhone { get; set; } = null!;

        public long EmployeeId { get; set; }

        public string EmployeeFirstName { get; set; } = null!;

        public string EmployeeLastName { get; set; } = null!;

        public DateTimeOffset StartsAt { get; set; }

        public DateTimeOffset EndsAt { get; set; }

        public int TotalDurationMinutes { get; set; }

        public decimal TotalPrice { get; set; }

        public BookingStatus Status { get; set; }

        public string? Notes { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
