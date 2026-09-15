using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Bookings.Request
{
    public sealed class CreateBookingRequest
    {
        [Range(1, long.MaxValue, ErrorMessage = "Employee ID must be greater than 0.")]
        public long EmployeeId { get; init; }

        [Required]
        public DateTimeOffset StartsAt { get; init; }

        [MinLength(1, ErrorMessage = "At least one service must be selected.")]
        public IReadOnlyList<long> ServiceIds { get; init; }
            = [];

        [MaxLength(500)]
        public string? Notes { get; init; }
    }
}
