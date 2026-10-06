using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Auth.Request
{
    public sealed class ResetPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Required]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "Kod mora sadržavati tačno 6 cifara.")]
        public string Code { get; init; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; init; } = string.Empty;

        [Required]
        [Compare(
            nameof(NewPassword),
            ErrorMessage = "Lozinke se ne podudaraju.")]
        public string ConfirmPassword { get; init; } = string.Empty;
    }
}
