using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Salons.Request
{
    public sealed class CreateSalonRequest : IValidatableObject
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public required string Name { get; init; }

        [Required]
        [StringLength(255, MinimumLength = 2)]
        public required string Address { get; init; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public required string City { get; init; }

        [Url]
        [StringLength(255)]
        public string? InstagramUrl { get; init; }

        [Url]
        [StringLength(255)]
        public string? FacebookUrl { get; init; }

        [Range(typeof(decimal), "-90", "90")]
        public decimal? Latitude { get; init; }

        [Range(typeof(decimal), "-180", "180")]
        public decimal? Longitude { get; init; }

        public bool IsActive { get; init; } = true;

        public IReadOnlyCollection<CreateSalonWorkingHoursRequest> WorkingHours { get; init; }
            = [];

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (Latitude.HasValue != Longitude.HasValue)
            {
                yield return new ValidationResult(
                    "Latitude and longitude must either both be provided or both be null.",
                    [nameof(Latitude), nameof(Longitude)]);
            }
        }
    }
}
