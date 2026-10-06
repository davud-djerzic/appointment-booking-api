using AppointmentBooking.Api.DTOs.Auth.Request;
using AppointmentBooking.Api.DTOs.Auth.Response;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Services.Auth;
using AppointmentBooking.Api.Services.PasswordReset;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController(IAuthService authService, IPasswordResetService passwordResetService, IWebHostEnvironment environment) : ControllerBase
    {
        [HttpPost("register")]
        [ProducesResponseType<TokenResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthenticationTokenResponse>> Register(RegisterCustomerRequest request, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await authService.RegisterCustomerAsync(request, cancellationToken);

            Response.Cookies.Append(
                "refreshToken",
                result.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !environment.IsDevelopment(),
                    SameSite = SameSiteMode.Lax,
                    Expires = result.RefreshTokenExpiresAt,
                    Path = "/api/auth"
                });

            AuthenticationTokenResponse response = new(
                result.AccessToken,
                result.AccessTokenExpiresAt,
                result.RefreshToken,
                result.RefreshTokenExpiresAt);

            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }

        [HttpPost("login")]
        [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthenticationTokenResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
        {
            AuthenticationResult result = await authService.LoginAsync(request, cancellationToken);

            Response.Cookies.Append(
                "refreshToken",
                result.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !environment.IsDevelopment(),
                    SameSite = SameSiteMode.Lax,
                    Expires = result.RefreshTokenExpiresAt,
                    Path = "/api/auth"
                });

            AuthenticationTokenResponse response = new(
                result.AccessToken,
                result.AccessTokenExpiresAt,
                result.RefreshToken,
                result.RefreshTokenExpiresAt);

            return Ok(response);
        }

        [HttpPost("refresh")]
        [ProducesResponseType<AuthenticationTokenResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AuthenticationTokenResponse>> Refresh([FromBody] RefreshTokenRequest? request,CancellationToken cancellationToken)
        {
            string? refreshToken = request?.RefreshToken;

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                Request.Cookies.TryGetValue(
                    "refreshToken",
                    out refreshToken);
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new UnauthorizedException(
                    "Invalid refresh token.");
            }

            AuthenticationResult result =
                await authService.RefreshAsync(
                    refreshToken,
                    cancellationToken);

            Response.Cookies.Append(
                "refreshToken",
                result.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !environment.IsDevelopment(),
                    SameSite = SameSiteMode.Lax,
                    Expires = result.RefreshTokenExpiresAt,
                    Path = "/api/auth"
                });

            AuthenticationTokenResponse response = new(
                result.AccessToken,
                result.AccessTokenExpiresAt,
                result.RefreshToken,
                result.RefreshTokenExpiresAt);

            return Ok(response);
        }

        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout(
            [FromBody] RefreshTokenRequest? request,
            CancellationToken cancellationToken)
        {
            string? refreshToken = request?.RefreshToken;

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                Request.Cookies.TryGetValue(
                    "refreshToken",
                    out refreshToken);
            }

            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await authService.LogoutAsync(
                    refreshToken,
                    cancellationToken);
            }

            Response.Cookies.Delete(
                "refreshToken",
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !environment.IsDevelopment(),
                    SameSite = SameSiteMode.Lax,
                    Path = "/api/auth"
                });

            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            await passwordResetService.RequestResetAsync(
                request.Email,
                cancellationToken);

            return Ok(new
            {
                message =
                    "Ako račun sa unesenom email adresom postoji, "
                    + "na tu adresu je poslan kod za promjenu lozinke."
            });
        }

        [AllowAnonymous]
        [HttpPost("verify-reset-code")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyResetCode(VerifyResetCodeRequest request, CancellationToken cancellationToken)
        {
            await passwordResetService.VerifyCodeAsync(
                request.Email,
                request.Code,
                cancellationToken);

            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request,CancellationToken cancellationToken)
        {
            await passwordResetService.ResetPasswordAsync(
                request.Email,
                request.Code,
                request.NewPassword,
                request.ConfirmPassword,
                cancellationToken);

            return NoContent();
        }
    }
}
