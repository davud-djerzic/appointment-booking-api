using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.Repositories.Appointments
{
    public sealed class AppointmentListItem
    {
        public long Id { get; init; }

        public long BookingId { get; init; }

        public long CustomerId { get; init; }

        public string CustomerFirstName { get; init; } = null!;

        public string CustomerLastName { get; init; } = null!;

        public string CustomerEmail { get; init; } = null!;

        public string CustomerPhone { get; init; } = null!;

        public long EmployeeId { get; init; }

        public string EmployeeFirstName { get; init; } = null!;

        public string EmployeeLastName { get; init; } = null!;

        public long ServiceId { get; init; }

        public string ServiceName { get; init; } = null!;

        public DateTimeOffset StartsAt { get; init; }

        public DateTimeOffset EndsAt { get; init; }

        public AppointmentStatus Status { get; init; }

        public string? Notes { get; init; }

        public string EmployeeFullName =>
            $"{EmployeeFirstName} {EmployeeLastName}";

        public string CustomerFullName =>
            $"{CustomerFirstName} {CustomerLastName}";
    }
}
