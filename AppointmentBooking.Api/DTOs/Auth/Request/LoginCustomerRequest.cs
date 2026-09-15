using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Auth.Request
{
    public sealed class LoginCustomerRequest
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email format is invalid.")]
        [StringLength(254)]
        public required string Email { get; init; }

        [Required(ErrorMessage = "Password is required.")]
        public required string Password { get; init; }
    }
}
