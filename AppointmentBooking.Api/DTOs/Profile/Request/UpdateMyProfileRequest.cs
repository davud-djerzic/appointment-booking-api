using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Profile.Request
{
    public sealed class UpdateMyProfileRequest
    {
        [StringLength(100, MinimumLength = 2, ErrorMessage = "First name must be between 2 and 100 characters.")]
        public string? FirstName { get; init; }

        [StringLength(100, MinimumLength = 2, ErrorMessage = "Last name must be between 2 and 100 characters.")]
        public string? LastName { get; init; }

        [EmailAddress(ErrorMessage = "The email address is not valid.")]
        [StringLength(254, ErrorMessage = "Email address cannot exceed 254 characters.")]
        public string? Email { get; init; }

        [Phone(ErrorMessage = "The phone number is not valid.")]
        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
        public string? Phone { get; init; }
    }

}
