using AppointmentBooking.Api.DTOs.Profile.Request;
using AppointmentBooking.Api.DTOs.Profile.Response;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.Auth;
using AppointmentBooking.Api.Repositories.Customers;
using AppointmentBooking.Api.Repositories.UserAccounts;
using AppointmentBooking.Api.Services.CurrentUser;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.Services.Profile
{
    public sealed class ProfileService(ICustomerRepository customerRepository, IUserAccountRepository userAccountRepository, ICurrentUserService currentUser, IPasswordHasher<UserAccount> passwordHasher, IAuthRepository authRepository) : IProfileService
    {
        public async Task<MyProfileResponse> GetMyProfileAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UserAccount? userAccount = await userAccountRepository.GetByIdAsync(currentUser.UserAccountId, cancellationToken);

            if (userAccount is null || !userAccount.IsActive) throw new UnauthorizedException("The current user account is inactive or could not be found.");
            

            Customer? customer = await customerRepository.GetActiveByUserAccountIdAsync(currentUser.UserAccountId,cancellationToken);

            if (customer is null) throw new UnauthorizedException("The customer profile is inactive or could not be found.");
            

            return new MyProfileResponse(
                userAccount.FirstName,
                userAccount.LastName,
                userAccount.Email,
                customer.Phone);
        }

        public async Task<MyProfileResponse> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool hasChanges =
                request.FirstName is not null ||
                request.LastName is not null ||
                request.Email is not null ||
                request.Phone is not null;

            if (!hasChanges)
            {
                throw new ValidationException(
                    "At least one profile field must be provided.");
            }

            await customerRepository.UpdateProfileAsync(
                currentUser.UserAccountId,
                request,
                cancellationToken);

            return await GetMyProfileAsync(cancellationToken);
        }

        public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            UserAccount? userAccount =
                await userAccountRepository.GetByIdAsync(
                    currentUser.UserAccountId,
                    cancellationToken);

            if (userAccount is null || !userAccount.IsActive)
            {
                throw new UnauthorizedException(
                    "The current user account is inactive or could not be found.");
            }

            PasswordVerificationResult passwordResult =
                passwordHasher.VerifyHashedPassword(
                    userAccount,
                    userAccount.PasswordHash,
                    request.CurrentPassword);

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                throw new ValidationException(
                    "Unable to change password. Please check your current password and try again.");
            }

            if (string.Equals(
                    request.CurrentPassword,
                    request.NewPassword,
                    StringComparison.Ordinal))
            {
                throw new ValidationException(
                    "Unable to change password. Please try again.");
            }

            string newPasswordHash =
                passwordHasher.HashPassword(
                    userAccount,
                    request.NewPassword);

            bool updated =
                await userAccountRepository.UpdatePasswordHashAsync(
                    currentUser.UserAccountId,
                    newPasswordHash,
                    cancellationToken);

            if (!updated)
            {
                throw new UnauthorizedException(
                    "The current user account is inactive or could not be found.");
            }

            await authRepository.RevokeAllRefreshTokensAsync(
                currentUser.UserAccountId,
                cancellationToken);
        }
    }
}
