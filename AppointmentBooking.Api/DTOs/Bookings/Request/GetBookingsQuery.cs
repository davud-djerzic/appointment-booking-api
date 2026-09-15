using AppointmentBooking.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Bookings.Request
{
    public sealed class GetBookingsQuery : IValidatableObject
    {
        public BookingStatus? Status { get; init; }

        public DateTimeOffset? From { get; init; }

        public DateTimeOffset? To { get; init; }

        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 20;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (From.HasValue && To.HasValue && From > To)
            {
                yield return new ValidationResult(
                    "From cannot be greater than To.",
                    [nameof(From), nameof(To)]);
            }
        }
    }
}
