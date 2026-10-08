using AppointmentBooking.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.EmployeeUnavailability.Request
{
    public sealed class SetEmployeeUnavailabilityRequest : IValidatableObject
    {
        public EmployeeUnavailabilityType Type { get; init; }

        [Required]
        public DateTimeOffset StartsAt { get; init; }

        [Required]
        public DateTimeOffset EndsAt { get; init; }

        public bool IsAllDay { get; init; }

        [MaxLength(500)]
        public string? Note { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Enum.IsDefined(Type))
            {
                yield return new ValidationResult(
                    "Type must be Break or Vacation.",
                    [nameof(Type)]);
            }

            if (EndsAt <= StartsAt)
            {
                yield return new ValidationResult(
                    "EndsAt must be later than StartsAt.",
                    [nameof(EndsAt)]);
            }

            if (Type == EmployeeUnavailabilityType.Break)
            {
                if (IsAllDay)
                {
                    yield return new ValidationResult(
                        "A break must have a start and end time.",
                        [nameof(IsAllDay)]);
                }

                if (StartsAt.Date != EndsAt.Date)
                {
                    yield return new ValidationResult(
                        "A break must start and end on the same date.",
                        [nameof(EndsAt)]);
                }
            }

            if (Type == EmployeeUnavailabilityType.Vacation)
            {
                if (!IsAllDay)
                {
                    yield return new ValidationResult(
                        "Vacation must be an all-day block.",
                        [nameof(IsAllDay)]);
                }

                if (StartsAt.TimeOfDay != TimeSpan.Zero ||
                    EndsAt.TimeOfDay != TimeSpan.Zero)
                {
                    yield return new ValidationResult(
                        "All-day vacation must start and end at local midnight.",
                        [nameof(StartsAt), nameof(EndsAt)]);
                }
            }
        }
    }
}
