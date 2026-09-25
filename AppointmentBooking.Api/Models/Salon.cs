namespace AppointmentBooking.Api.Models
{
    public sealed class Salon
    {
        public long Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Address { get; init; } = string.Empty;

        public string City { get; init; } = string.Empty;

        public string? InstagramUrl { get; init; }

        public string? FacebookUrl { get; init; }

        public decimal? Latitude { get; init; }

        public decimal? Longitude { get; init; }

        public bool IsActive { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset UpdatedAt { get; init; }
    }
}
