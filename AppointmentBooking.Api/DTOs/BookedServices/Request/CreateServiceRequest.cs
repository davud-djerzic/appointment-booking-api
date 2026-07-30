using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.BookedServices.Request
{
    public sealed class CreateServiceRequest
    {
        [Required(ErrorMessage = "Service name is required.")]
        [StringLength(100, MinimumLength = 2)]
        public required string Name { get; init; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; init; }

        [Required(ErrorMessage = "Duration in minutes is required.")]
        [Range(1, 600, ErrorMessage = "Duration must be between 1 and 600 minutes.")]
        public required int DurationMinutes { get; init; }

        [Range(typeof(decimal), "0.00", "99999999.99", ErrorMessage = "Price must be between 0.00 and 99999999.99")]
        public required decimal Price { get; init; }
    }
}
