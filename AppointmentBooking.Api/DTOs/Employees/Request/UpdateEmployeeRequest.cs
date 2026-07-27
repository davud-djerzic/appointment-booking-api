using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Employees.Request
{
    public sealed class UpdateEmployeeRequest
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(
        50,
        MinimumLength = 2,
        ErrorMessage = "First name must contain between 2 and 50 characters.")]
        public required string FirstName { get; init; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(
           50,
           MinimumLength = 2,
           ErrorMessage = "Last name must contain between 2 and 50 characters.")]
        public required string LastName { get; init; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email format is invalid.")]
        [StringLength(254)]
        public required string Email { get; init; }

        [Required(ErrorMessage = "Phone is required.")]
        [StringLength(
            30,
            MinimumLength = 6,
            ErrorMessage = "Phone must contain between 6 and 30 characters.")]
        public required string Phone { get; init; }
    }
}
