namespace AppointmentBooking.Api.Models
{
    public class PagedResult<T>
    {
        public required IReadOnlyCollection<T> Items { get; init; }
        public int TotalCount { get; init; }
    }
}
