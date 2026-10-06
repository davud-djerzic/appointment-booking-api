namespace AppointmentBooking.Api.Services.PasswordReset
{
    public interface IPasswordResetService
    {
        Task RequestResetAsync(string email,CancellationToken cancellationToken);
        Task VerifyCodeAsync(string email, string code, CancellationToken cancellationToken);

        Task ResetPasswordAsync(string email, string code, string newPassword, string confirmPassword, CancellationToken cancellationToken);
    }
}
