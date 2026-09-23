using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Profile.Request
{
    public sealed class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Current password is required.")]
        public required string CurrentPassword { get; init; }

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "New password must contain between 8 and 100 characters.")]
        public required string NewPassword { get; init; }
    }
}
