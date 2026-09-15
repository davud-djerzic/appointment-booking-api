namespace AppointmentBooking.Api.Repositories.Auth
{
    public sealed record RefreshTokenRotationResult(long UserAccountId, Guid FamilyId, long NewRefreshTokenId);
}
