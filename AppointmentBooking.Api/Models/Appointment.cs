using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.Models
{
    public class Appointment
    {
        public long Id { get; init; }

        public long BookingId { get; init; }

        public long ServiceId { get; init; }

        public string ServiceNameAtBooking { get; init; } = null!;

        public int DurationMinutesAtBooking { get; init; }

        public decimal PriceAtBooking { get; init; }

        public DateTimeOffset StartsAt { get; init; }

        public DateTimeOffset EndsAt { get; init; }

        public AppointmentStatus Status { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
