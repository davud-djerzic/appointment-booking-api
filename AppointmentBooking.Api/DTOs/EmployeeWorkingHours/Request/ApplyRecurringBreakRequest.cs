using AppointmentBooking.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Request;

public sealed class ApplyRecurringBreakRequest : IValidatableObject
{
    public WeekDay DayOfWeek { get; init; }

    public TimeOnly StartsAt { get; init; }

    public TimeOnly EndsAt { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.IsDefined(DayOfWeek) || DayOfWeek is WeekDay.Saturday or WeekDay.Sunday)
        {
            yield return new ValidationResult(
                "DayOfWeek must be a working weekday from Monday to Friday.",
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
