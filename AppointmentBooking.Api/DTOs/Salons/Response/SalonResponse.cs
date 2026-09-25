namespace AppointmentBooking.Api.DTOs.Salons.Response
{
    public sealed record SalonResponse(
        long Id,
        string Name,
        string Address,
        string City,
        string? InstagramUrl,
        string? FacebookUrl,
        decimal? Latitude,
        decimal? Longitude,
        bool IsActive,
        IReadOnlyCollection<SalonWorkingHoursResponse> WorkingHours);
}
