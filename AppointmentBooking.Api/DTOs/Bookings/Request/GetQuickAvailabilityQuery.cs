using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Bookings.Request
{
    public sealed class GetQuickAvailabilityQuery
    {
        [Range(1, long.MaxValue)]
        public long ServiceId { get; init; }
    }
}
