using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Appointments
{
    public sealed class AppointmentListItem
    {
        public long Id { get; init; }

        public long EmployeeId { get; init; }

        public string EmployeeFirstName { get; init; } = default!;

        public string EmployeeLastName { get; init; } = default!;

        public long ServiceId { get; init; }

        public string ServiceName { get; init; } = default!;

        public string? CustomerFirstName { get; init; }

        public string? CustomerLastName { get; init; }

        public string? CustomerEmail { get; init; }

        public string? CustomerPhone { get; init; }

        public DateTimeOffset StartsAt { get; init; }

        public DateTimeOffset EndsAt { get; init; }

        public AppointmentStatus Status { get; init; }

        public string? Notes { get; init; }

        public string EmployeeFullName =>
            $"{EmployeeFirstName} {EmployeeLastName}";
    }
}
