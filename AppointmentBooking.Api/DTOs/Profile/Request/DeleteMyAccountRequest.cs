using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Profile.Request;

public sealed class DeleteMyAccountRequest
{
    [Required(ErrorMessage = "Current password is required.")]
    public required string CurrentPassword { get; init; }
}
