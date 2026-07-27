namespace AppointmentBooking.Api.DTOs.Common
{
    public sealed class PagedResponse<T>
    {
        public required IReadOnlyCollection<T> items { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }

        public int TotalCount { get; init; }

        public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
