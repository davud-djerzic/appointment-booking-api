using AppointmentBooking.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Request
{
    public sealed class CreateEmployeeWorkingHoursRequest : IValidatableObject
    {
        [Required]
        public WeekDay DayOfWeek { get; init; }

        [Required]
        public TimeOnly StartsAt { get; init; }

        [Required]
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
