using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Bookings.Request;

public sealed class GetBookingCalendarDaysQuery : IValidatableObject
{
    public long EmployeeId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EmployeeId <= 0)
        {
            yield return new ValidationResult("EmployeeId must be greater than zero.", [nameof(EmployeeId)]);
        }
    }
}
