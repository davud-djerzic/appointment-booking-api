namespace AppointmentBooking.Api.Repositories.BookedServices
{
    public sealed record UpdateServiceData
    {
        public long Id { get; init; }

        public required string Name { get; init; }

        public string? Description { get; init; }

        public required int DurationMinutes { get; init; }

        public required decimal Price { get; init; }
    }
}
