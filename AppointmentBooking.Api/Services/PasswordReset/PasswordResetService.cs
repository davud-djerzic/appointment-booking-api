using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.Auth;
using AppointmentBooking.Api.Repositories.PasswordResetCodes;
using AppointmentBooking.Api.Repositories.UserAccounts;
using AppointmentBooking.Api.Services.Email;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace AppointmentBooking.Api.Services.PasswordReset
{
    public sealed class PasswordResetService(IUserAccountRepository userAccountRepository, IPasswordResetCodeRepository passwordResetCodeRepository, IAuthRepository authRepository,IEmailService emailService, IPasswordHasher<UserAccount> passwordHasher) : IPasswordResetService
    {
        private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

        private const short MaxAttempts = 5;

        public async Task RequestResetAsync(string email, CancellationToken cancellationToken)
        {
            string normalizedEmail = email.Trim();

            UserAccount? userAccount = await userAccountRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

            if (userAccount is null || !userAccount.IsActive) return;

            await passwordResetCodeRepository.InvalidateActiveAsync(userAccount.Id, cancellationToken);

            int code = RandomNumberGenerator.GetInt32(100000, 1000000);

            string codeText = code.ToString();

            string codeHash =
                passwordHasher.HashPassword(
                    userAccount,
                    codeText);

            PasswordResetCode passwordResetCode = new()
            {
                UserAccountId = userAccount.Id,
                CodeHash = codeHash,
                ExpiresAt = DateTimeOffset.UtcNow.Add(CodeLifetime),
                Attempts = 0,
                VerifiedAt = null,
                UsedAt = null,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await passwordResetCodeRepository.CreateAsync(
                passwordResetCode,
                cancellationToken);

            await emailService.SendAsync(
                toEmail: userAccount.Email,
                toName: $"{userAccount.FirstName} {userAccount.LastName}",
                subject: "Kod za promjenu lozinke",
                textPart: $"""
                       Pozdrav {userAccount.FirstName},

                       Vaš kod za promjenu lozinke je:

                       {codeText}

                       Kod vrijedi 10 minuta.

                       Ako niste zatražili promjenu lozinke, možete ignorisati ovu poruku.

                       Lijep pozdrav,
                       Salon
                       """,
                htmlPart: $"""
                       <h2>Promjena lozinke</h2>

                       <p>Pozdrav {userAccount.FirstName},</p>

                       <p>Vaš kod za promjenu lozinke je:</p>

                       <div style="
                           font-size: 32px;
                           font-weight: bold;
                           letter-spacing: 8px;
                           margin: 20px 0;">
                           {codeText}
                       </div>

                       <p>Kod vrijedi <strong>10 minuta</strong>.</p>

                       <p>
                           Ako niste zatražili promjenu lozinke,
                           možete ignorisati ovu poruku.
                       </p>

                       <p>
                           Lijep pozdrav,<br/>
                           Salon
                       </p>
                       """,
                cancellationToken);

        }
        public async Task VerifyCodeAsync(string email, string code, CancellationToken cancellationToken)
        {
            string normalizedEmail = email.Trim();

            UserAccount? userAccount =
                await userAccountRepository.GetByEmailAsync(
                    normalizedEmail,
                    cancellationToken);

            if (userAccount is null || !userAccount.IsActive) throw new ValidationException("Kod nije ispravan ili je istekao.");
            

            PasswordResetCode? resetCode =
                await passwordResetCodeRepository.GetLatestActiveAsync(
                    userAccount.Id,
                    cancellationToken);

            if (resetCode is null) throw new ValidationException("Kod nije ispravan ili je istekao.");
            

            if (resetCode.VerifiedAt is not null) return;
            

            bool isValid =
                passwordHasher.VerifyHashedPassword(
                    userAccount,
                    resetCode.CodeHash,
                    code.Trim())
                != PasswordVerificationResult.Failed;

            if (!isValid)
            {
                bool incremented =
                    await passwordResetCodeRepository.IncrementAttemptsAsync(
                        resetCode.Id,
                        cancellationToken);

                if (!incremented)
                {
                    throw new ValidationException(
                        "Prekoračen je dozvoljeni broj pokušaja.");
                }

                throw new ValidationException(
                    "Kod nije ispravan.");
            }

            bool verified =
                await passwordResetCodeRepository.MarkVerifiedAsync(
                    resetCode.Id,
                    cancellationToken);

            if (!verified)
            {
                throw new ValidationException(
                    "Kod više nije važeći.");
            }
        }

        public async Task ResetPasswordAsync(string email, string code, string newPassword, string confirmPassword, CancellationToken cancellationToken)
        {
            if (newPassword != confirmPassword) throw new ValidationException("Lozinke se ne podudaraju.");

            string normalizedEmail = email.Trim();

            UserAccount? userAccount =
                await userAccountRepository.GetByEmailAsync(
                    normalizedEmail,
                    cancellationToken);

            if (userAccount is null || !userAccount.IsActive)
            {
                throw new ValidationException(
                    "Zahtjev za promjenu lozinke nije važeći.");
            }

            PasswordResetCode? resetCode =
                await passwordResetCodeRepository.GetLatestActiveAsync(
                    userAccount.Id,
                    cancellationToken);

            if (resetCode is null ||
                resetCode.VerifiedAt is null)
            {
                throw new ValidationException(
                    "Prvo morate potvrditi kod.");
            }

            bool isValid =
                passwordHasher.VerifyHashedPassword(
                    userAccount,
                    resetCode.CodeHash,
                    code.Trim())
                != PasswordVerificationResult.Failed;

            if (!isValid)
            {
                throw new ValidationException(
                    "Zahtjev za promjenu lozinke nije važeći.");
            }

            string passwordHash =
                passwordHasher.HashPassword(
                    userAccount,
                    newPassword);

            bool passwordUpdated =
                await userAccountRepository.UpdatePasswordHashAsync(
                    userAccount.Id,
                    passwordHash,
                    cancellationToken);

            if (!passwordUpdated)
            {
                throw new ValidationException(
                    "Lozinka nije uspješno promijenjena.");
            }

            await passwordResetCodeRepository.MarkUsedAsync(
                resetCode.Id,
                cancellationToken);

            await authRepository.RevokeAllRefreshTokensAsync(
                userAccount.Id,
                cancellationToken);

            await emailService.SendAsync(
                toEmail: userAccount.Email,
                toName: $"{userAccount.FirstName} {userAccount.LastName}",
                subject: "Lozinka je promijenjena",
                textPart: $"""
                       Pozdrav {userAccount.FirstName},

                       Vaša lozinka je uspješno promijenjena.

                       Ako ovu promjenu niste izvršili Vi,
                       odmah kontaktirajte podršku.

                       Lijep pozdrav,
                       Salon
                       """,
                htmlPart: $"""
                       <h2>Lozinka je promijenjena</h2>

                       <p>Pozdrav {userAccount.FirstName},</p>

                       <p>
                           Vaša lozinka je uspješno promijenjena.
                       </p>

                       <p>
                           Ako ovu promjenu niste izvršili Vi,
                           odmah kontaktirajte podršku.
                       </p>

                       <p>
                           Lijep pozdrav,<br/>
                           Salon
                       </p>
                       """,
                cancellationToken);
        }
    }
}
