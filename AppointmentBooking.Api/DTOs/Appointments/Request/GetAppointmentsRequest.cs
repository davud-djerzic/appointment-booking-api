using AppointmentBooking.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.DTOs.Appointments.Request
{
    public sealed class GetAppointmentsRequest
    {
        [Range(1, long.MaxValue)]
        public long? EmployeeId { get; init; }

        [Range(1, long.MaxValue)]
        public long? ServiceId { get; init; }

        public AppointmentStatus? Status { get; init; }

        public DateOnly? Date { get; init; }

        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 20;
    }
 
}
