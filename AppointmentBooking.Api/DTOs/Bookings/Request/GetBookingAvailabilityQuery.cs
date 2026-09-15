using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Bookings.Request
{
    public sealed class GetBookingAvailabilityQuery : IValidatableObject
    {
        public long EmployeeId { get; init; }

        public DateOnly Date { get; init; }

        public List<long> ServiceIds { get; init; } = [];

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (EmployeeId <= 0)
            {
                yield return new ValidationResult(
                    "EmployeeId must be greater than zero.",
                    new[] { nameof(EmployeeId) });
            }

            if (Date == default)
            {
                yield return new ValidationResult(
                    "Date is required.",
                    new[] { nameof(Date) });
            }

            if (ServiceIds.Count == 0)
            {
                yield return new ValidationResult(
                    "At least one service must be selected.",
                    new[] { nameof(ServiceIds) });
            }

            if (ServiceIds.Any(id => id <= 0))
            {
                yield return new ValidationResult(
                    "Service IDs must be greater than zero.",
                    new[] { nameof(ServiceIds) });
            }

            if (ServiceIds.Distinct().Count() != ServiceIds.Count)
            {
                yield return new ValidationResult(
                    "Service IDs must be unique.",
                    new[] { nameof(ServiceIds) });
            }
        }
    
    }
}
