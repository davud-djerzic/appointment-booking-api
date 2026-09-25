using AppointmentBooking.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Salons.Request
{
    public sealed class CreateSalonWorkingHoursRequest : IValidatableObject
    {
        public WeekDay DayOfWeek { get; init; }

        public TimeOnly StartsAt { get; init; }

        public TimeOnly EndsAt { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Enum.IsDefined(DayOfWeek))
            {
                yield return new ValidationResult(
                    "DayOfWeek must be a valid weekday.",
                    [nameof(DayOfWeek)]);
            }
            if (EndsAt <= StartsAt)
            {
                yield return new ValidationResult(
                    "EndsAt must be later than StartsAt.",
                    [nameof(EndsAt)]);
            }
        }
    }
}
