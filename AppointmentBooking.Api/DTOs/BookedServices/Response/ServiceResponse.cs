namespace AppointmentBooking.Api.DTOs.BookedServices.Response
{
    public sealed class ServiceResponse
    {
        public long Id { get; set; }
        public required string Name { get; init; }

        public string? Description { get; init; }
        public required int DurationMinutes { get; init; }

        public required decimal Price { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
