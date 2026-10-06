using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Auth.Request
{
    public sealed class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; init; } = string.Empty;
    }
}
