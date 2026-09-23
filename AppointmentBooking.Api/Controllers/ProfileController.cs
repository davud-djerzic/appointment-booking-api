using AppointmentBooking.Api.DTOs.Profile.Request;
using AppointmentBooking.Api.DTOs.Profile.Response;
using AppointmentBooking.Api.Services.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize(Roles = "Customer")]
    public sealed class ProfileController(IProfileService profileService) : ControllerBase
    {
        [HttpGet("me")]
        [ProducesResponseType<MyProfileResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MyProfileResponse>> GetMyProfile(CancellationToken cancellationToken)
        {
            MyProfileResponse response = await profileService.GetMyProfileAsync(cancellationToken);

            return Ok(response);
        }

        [HttpPatch("me")]
        [ProducesResponseType<MyProfileResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<MyProfileResponse>> UpdateMyProfile(UpdateMyProfileRequest request, CancellationToken cancellationToken)
        {
            MyProfileResponse response = await profileService.UpdateMyProfileAsync(request, cancellationToken);

            return Ok(response);
        }

        [HttpPost("me/change-password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            await profileService.ChangePasswordAsync(
                request,
                cancellationToken);

            return NoContent();
        }
    }
}

