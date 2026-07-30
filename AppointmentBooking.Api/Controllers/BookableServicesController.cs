using AppointmentBooking.Api.DTOs.BookedServices.Request;
using AppointmentBooking.Api.DTOs.BookedServices.Response;
using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.Services.BookedServices;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class BookableServicesController(IBookableServiceService bookableService) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<ServiceResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ServiceResponse>> Create(CreateServiceRequest request, CancellationToken cancellationToken)
        {
            ServiceResponse response = await bookableService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpGet("{id:long}")]
        [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ServiceResponse>> GetById(long id, CancellationToken cancellationToken)
        {
            var service = await bookableService.GetByIdAsync(id, cancellationToken);

            if (service is null) return NotFound(new ProblemDetails {
                Status = StatusCodes.Status404NotFound,
                Title = "Service not found",
                Detail = $"Service with ID '{id}' was not found.",
                Instance = HttpContext.Request.Path
            });

            return Ok(service);
        }

        [HttpDelete("{id:long:min(1)}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            bool deactivated = await bookableService.DeactivateAsync(id, cancellationToken);
            if (!deactivated)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Service not found",
                    Detail = $"Service with ID '{id}' was not found.",
                    Instance = HttpContext.Request.Path
                });
            }

            return NoContent();
        }

        [HttpGet]
        [ProducesResponseType<PagedResponse<ServiceResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResponse<ServiceResponse>>> GetAll([FromQuery] GetServicesQuery query, CancellationToken cancellationToken)
        {
            var result = await bookableService.GetAllAsync(query, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{id:long:min(1)}")]
        [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ServiceResponse>> Update(long id, UpdateServiceRequest request, CancellationToken cancellationToken)
        {
            ServiceResponse? response = await bookableService.UpdateAsync(id, request, cancellationToken);
            if (response is null) return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Employee not found",
                Detail = $"Employee with ID '{id}' was not found.",
                Instance = HttpContext.Request.Path
            });

            return Ok(response);
        }

        [HttpPost("{id:long:min(1)}/activate")]
        [ProducesResponseType<ServiceResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ServiceResponse>> Activate(long id, CancellationToken cancellationToken)
        {
            ServiceResponse? service = await bookableService.ActivateAsync(id, cancellationToken);

            if (service is null) return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Service not found",
                Detail = $"Service with ID '{id}' was not found.",
                Instance = HttpContext.Request.Path
            });

            return Ok(service);
        }
    }
}
