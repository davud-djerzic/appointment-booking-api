using AppointmentBooking.Api.DTOs.Appointments.Request;
using AppointmentBooking.Api.DTOs.Appointments.Response;
using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.Repositories.Appointments;
using AppointmentBooking.Api.Services.Appointments;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
    {
        /*[HttpPost("hold")]
        [ProducesResponseType<AppointmentHoldResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AppointmentHoldResponse>> Hold(CreateAppointmentHoldRequest request, CancellationToken cancellationToken)
        {
            AppointmentHoldResponse response = await appointmentService.CreateHoldAsync(request, cancellationToken);

            return Created($"/api/appointments/hold/{response.HoldToken}", response);
        }

        [HttpPost("{holdToken:guid}/confirm")]
        [ProducesResponseType<AppointmentResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AppointmentResponse>> Confirm(Guid holdToken, ConfirmHoldAppointment request, CancellationToken cancellationToken)
        {
            AppointmentResponse response = await appointmentService.ConfirmAsync(holdToken, request, cancellationToken);
            return Ok(response);
        }

        [HttpDelete("{holdToken:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(Guid holdToken, CancellationToken cancellationToken)
        {
            await appointmentService.DeleteHeldAsync(holdToken, cancellationToken);
            return NoContent();
        }

        [HttpPatch("{id:long:min(1)}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Cancel(long id, CancellationToken cancellationToken)
        {
            await appointmentService.CancelAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpPatch("{id:long:min(1)}/complete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Complete(long id, CancellationToken cancellationToken)
        {
            await appointmentService.CompleteAsync(id, cancellationToken);
            return NoContent();
        }

        [HttpGet]
        [ProducesResponseType<PagedResponse<AppointmentListItem>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AppointmentListItem>> GetAll([FromQuery] GetAppointmentsRequest request, CancellationToken cancellationToken)
        {
            PagedResponse<AppointmentListItem> response = await appointmentService.GetAllAsync(request, cancellationToken);
            return Ok(response);
        }*/
    }
}
