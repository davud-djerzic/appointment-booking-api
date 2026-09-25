using AppointmentBooking.Api.DTOs.Salons.Request;
using AppointmentBooking.Api.DTOs.Salons.Response;
using AppointmentBooking.Api.Services.Salons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/salons")]
    public sealed class SalonsController(ISalonService salonService) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<SalonResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SalonResponse>> Create(CreateSalonRequest request, CancellationToken cancellationToken)
        {
            SalonResponse response = await salonService.CreateAsync(request, cancellationToken);

            return Created($"/api/salons/{response.Id}", response);
        }

        [HttpGet("{salonId:long:min(1)}")]
        [ProducesResponseType<SalonResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SalonResponse>> GetById(long salonId, CancellationToken cancellationToken)
        {
            SalonResponse response = await salonService.GetByIdAsync(salonId, cancellationToken);

            return Ok(response);
        }
    }
}