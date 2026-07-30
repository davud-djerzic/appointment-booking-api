using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.BookedServices.Request
{
    public sealed class GetServicesQuery : IValidatableObject
    {
        [StringLength(100)]
        public string? Search { get; init; }
        public bool? IsActive { get; init; }

        [Range( typeof(decimal), "0.00", "99999999.99")]
        public decimal? MinPrice { get; init; }

        [Range(typeof(decimal), "0.00", "99999999.99")]
        public decimal? MaxPrice { get; init; }

        [Range(1, int.MaxValue)]
        public int? MinDuration { get; init; }
        [Range(1, int.MaxValue)]
        public int? MaxDuration { get; init; } 

        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;
        [Range(1, 100)]
        public int PageSize { get; init; } = 20;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
            {
                yield return new ValidationResult(
                    "MinPrice cannot be greater than MaxPrice.");
            }
            if (MinDuration.HasValue && MaxDuration.HasValue && MinDuration > MaxDuration)
            {
                yield return new ValidationResult(
                    "MinDuration cannot be greater than MaxDuration.");
            }
        }
    }
}
