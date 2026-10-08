using AppointmentBooking.Api.DTOs.EmployeeUnavailability.Request;
using AppointmentBooking.Api.DTOs.EmployeeUnavailability.Response;
using AppointmentBooking.Api.Services.EmployeeAvailabilityBlocks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Authorize(Roles = "Employee, Admin")]
    [Route("api/employees/me/availability-blocks")]
    public sealed class EmployeeUnavailabilityController(
        IEmployeeUnavailabilityService employeeUnavailabilityService)
        : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<IReadOnlyCollection<EmployeeUnavailabilityResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IReadOnlyCollection<EmployeeUnavailabilityResponse>>> GetMine(
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<EmployeeUnavailabilityResponse> response =
                await employeeUnavailabilityService.GetForCurrentEmployeeAsync(
                    cancellationToken);

            return Ok(response);
        }

        [HttpGet("{id:long:min(1)}")]
        [ProducesResponseType<EmployeeUnavailabilityResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeUnavailabilityResponse>> GetById(
            long id,
            CancellationToken cancellationToken)
        {
            EmployeeUnavailabilityResponse response =
                await employeeUnavailabilityService.GetForCurrentEmployeeAsync(
                    id,
                    cancellationToken);

            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType<EmployeeUnavailabilityResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EmployeeUnavailabilityResponse>> Create(
            SetEmployeeUnavailabilityRequest request,
            CancellationToken cancellationToken)
        {
            EmployeeUnavailabilityResponse response =
                await employeeUnavailabilityService.CreateForCurrentEmployeeAsync(
                    request,
                    cancellationToken);

            return Created(
                $"/api/employees/me/availability-blocks/{response.Id}",
                response);
        }

        [HttpPut("{id:long:min(1)}")]
        [ProducesResponseType<EmployeeUnavailabilityResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EmployeeUnavailabilityResponse>> Update(
            long id,
            SetEmployeeUnavailabilityRequest request,
            CancellationToken cancellationToken)
        {
            EmployeeUnavailabilityResponse response =
                await employeeUnavailabilityService.UpdateForCurrentEmployeeAsync(
                    id,
                    request,
                    cancellationToken);

            return Ok(response);
        }

        [HttpDelete("{id:long:min(1)}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(
            long id,
            CancellationToken cancellationToken)
        {
            await employeeUnavailabilityService.DeleteForCurrentEmployeeAsync(
                id,
                cancellationToken);

            return NoContent();
        }
    }
}
