using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Bookings.Request;

public sealed class CreateStaffBookingRequest
{
    [Required, StringLength(50)]
    public string CustomerFirstName { get; init; } = string.Empty;

    [Required, StringLength(50)]
    public string CustomerLastName { get; init; } = string.Empty;

    public long? EmployeeId { get; init; }

    [Required]
    public DateTimeOffset StartsAt { get; init; }

    [MinLength(1)]
    public IReadOnlyList<long> ServiceIds { get; init; } = [];

    [MaxLength(500)]
    public string? Notes { get; init; }
}
