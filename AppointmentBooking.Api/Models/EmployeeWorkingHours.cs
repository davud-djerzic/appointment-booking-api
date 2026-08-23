namespace AppointmentBooking.Api.Models
{
    public sealed class EmployeeWorkingHours
    {
        public long Id { get; init; }
        public long EmployeeId { get; init; }  
        public WeekDay DayOfWeek { get; init; }

        public TimeOnly StartsAt { get; init; }
        public TimeOnly EndsAt { get; init; }

    
    }
}
