namespace AppointmentBooking.Api.Models
{
    public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

}
