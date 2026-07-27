namespace AppointmentBooking.Api.Models
{
    public sealed class BookableService
    {
        public long Id { get; init; }

        public required string Name { get; init; }

        public string? Description { get; init; }
        
        public int DurationMinutes { get; init; }

        public decimal Priced { get; init; }
        public bool isActive { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }
}
