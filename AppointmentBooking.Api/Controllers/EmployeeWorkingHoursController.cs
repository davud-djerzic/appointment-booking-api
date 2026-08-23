using AppointmentBooking.Api.DTOs.BookedServices.Request;
using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Request;
using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Response;
using AppointmentBooking.Api.Services.EmployeeWorkingHoursService;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/employees/{employeeId:long:min(1)}/working-hours")]
    public sealed class EmployeeWorkingHoursController(IEmployeeWorkingHoursService employeeWorkingHoursService) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<EmployeeWorkingHoursResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>( StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>( StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployeeWorkingHoursResponse>> Create(
            long employeeId,
            CreateEmployeeWorkingHoursRequest request,
            CancellationToken cancellationToken)
        {
            EmployeeWorkingHoursResponse response =
                await employeeWorkingHoursService.CreateAsync(
                    employeeId,
                    request,
                    cancellationToken);

            return Created( $"/api/employees/{employeeId}/working-hours/{response.Id}", response);
        }

        [HttpGet("{workingHoursId:long:min(1)}")]
        [ProducesResponseType<EmployeeWorkingHoursResponse>( StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployeeWorkingHoursResponse>> GetById( long employeeId, long workingHoursId, CancellationToken cancellationToken)
        {
            EmployeeWorkingHoursResponse response = await employeeWorkingHoursService.GetByIdAsync(employeeId, workingHoursId, cancellationToken);

            return Ok(response);
        }

        [HttpGet]
        [ProducesResponseType<IReadOnlyCollection<EmployeeWorkingHoursResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>( StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>( StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyCollection<EmployeeWorkingHoursResponse>>> GetByEmployeeId( long employeeId, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<EmployeeWorkingHoursResponse> response = await employeeWorkingHoursService.GetByEmployeeIdAsync(employeeId, cancellationToken);

            return Ok(response);
        }

        [HttpPut("{workingHoursId:long:min(1)}")]
        [ProducesResponseType<EmployeeWorkingHoursResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>( StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(  StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>( StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>( StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployeeWorkingHoursResponse>> Update(long employeeId, long workingHoursId, UpdateEmployeeWorkingHoursRequest request, CancellationToken cancellationToken)
        {
            EmployeeWorkingHoursResponse response =
                await employeeWorkingHoursService.UpdateAsync(
                    employeeId,
                    workingHoursId,
                    request,
                    cancellationToken);

            return Ok(response);
        }

        [HttpDelete("{workingHoursId:long:min(1)}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>( StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(long employeeId,long workingHoursId, CancellationToken cancellationToken)
        {
            await employeeWorkingHoursService.DeleteAsync(employeeId, workingHoursId, cancellationToken);

            return NoContent();
        }
    }
}
