using AppointmentBooking.Api.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Bookings.Request
{
    public sealed class GetAdminBookingsQuery : IValidatableObject
    {
        public DateOnly? Date { get; init; }

        public DateOnly? From { get; init; }

        public DateOnly? To { get; init; }

        public long? EmployeeId { get; init; }

        public long? CustomerId { get; init; }

        public BookingStatus? Status { get; init; }

        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 20;

        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (Date.HasValue && (From.HasValue || To.HasValue))
            {
                yield return new ValidationResult(
                    "Date cannot be combined with From or To.");
            }

            if (From.HasValue &&
                To.HasValue &&
                From.Value > To.Value)
            {
                yield return new ValidationResult(
                    "From cannot be later than To.");
            }

            if (EmployeeId.HasValue && EmployeeId.Value <= 0)
            {
                yield return new ValidationResult(
                    "EmployeeId must be greater than zero.");
            }

            if (CustomerId.HasValue && CustomerId.Value <= 0)
            {
                yield return new ValidationResult(
                    "CustomerId must be greater than zero.");
            }

            if (Page < 1)
            {
                yield return new ValidationResult(
                    "Page must be greater than or equal to 1.");
            }

            if (PageSize < 1 || PageSize > 100)
            {
                yield return new ValidationResult(
                    "PageSize must be between 1 and 100.");
            }
        }
    }
}
