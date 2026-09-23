using AppointmentBooking.Api.DTOs.Profile.Request;
using AppointmentBooking.Api.DTOs.Profile.Response;

namespace AppointmentBooking.Api.Services.Profile
{
    public interface IProfileService
    {
        Task<MyProfileResponse> GetMyProfileAsync(CancellationToken cancellationToken);

        Task<MyProfileResponse> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken);

        Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken);
    }
}
