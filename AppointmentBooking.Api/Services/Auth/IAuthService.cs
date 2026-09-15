using AppointmentBooking.Api.DTOs.Auth.Request;

namespace AppointmentBooking.Api.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthenticationResult> RegisterCustomerAsync(RegisterCustomerRequest request, CancellationToken cancellationToken);

        Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

        Task<AuthenticationResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

        Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    }
}
