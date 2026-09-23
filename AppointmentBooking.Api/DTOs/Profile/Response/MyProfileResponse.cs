namespace AppointmentBooking.Api.DTOs.Profile.Response
{
    public sealed record MyProfileResponse(
        string FirstName,
        string LastName,
        string Email,
        string Phone);

}
