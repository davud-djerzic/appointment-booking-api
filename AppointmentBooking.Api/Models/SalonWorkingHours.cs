using AppointmentBooking.Api.Models.Enums;

namespace AppointmentBooking.Api.Models
{
    public sealed class SalonWorkingHours
    {
        public long Id { get; init; }

        public long SalonId { get; init; }

        public WeekDay DayOfWeek { get; init; }

        public TimeOnly StartsAt { get; init; }

        public TimeOnly EndsAt { get; init; }
    }
}
