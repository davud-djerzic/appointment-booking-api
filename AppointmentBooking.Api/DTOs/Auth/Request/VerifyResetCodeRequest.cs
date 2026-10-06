using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Auth.Request
{
    public sealed class VerifyResetCodeRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Required]
        [RegularExpression(
            @"^\d{6}$",
            ErrorMessage = "Kod mora sadržavati tačno 6 cifara.")]
        public string Code { get; init; } = string.Empty;
    }
}
