using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Repositories.Appointments
{
    public class AppointmentSearchCriteria
    {
        public long? EmployeeId { get; init; }
        public long? ServiceId { get; init; }
        public AppointmentStatus? Status { get; init; } 
        public DateOnly? Date { get; init; }
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 20;

}
}
