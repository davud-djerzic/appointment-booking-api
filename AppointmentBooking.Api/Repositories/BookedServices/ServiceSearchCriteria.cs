namespace AppointmentBooking.Api.Repositories.BookedServices
{
    public sealed class ServiceSearchCriteria
    {
        public string? Search { get; init; }

        public bool? IsActive { get; init; }

        public decimal? MinPrice { get; init; }

        public decimal? MaxPrice { get; init; }

        public int? MinDuration { get; init; }

        public int? MaxDuration { get; init; }

        public int Page { get; init; }

        public int PageSize { get; init; }
    }
}
