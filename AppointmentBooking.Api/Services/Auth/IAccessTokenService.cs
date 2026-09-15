using AppointmentBooking.Api.Models;

namespace AppointmentBooking.Api.Services.Auth
{
    public interface IAccessTokenService
    {
        AccessTokenResult CreateAccessToken(UserAccount userAccount);
    }
}
