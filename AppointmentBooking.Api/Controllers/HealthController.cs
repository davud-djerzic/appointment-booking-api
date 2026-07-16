
using AppointmentBooking.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class HealthController(IDatabaseHealthRepository databaseHealthRepository) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            var databaseIsConnected = await databaseHealthRepository.CanConnectAsync(cancellationToken);

            if (!databaseIsConnected)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        status = "Unhealthy",
                        database = "Disconnected",
                        timestamp = DateTimeOffset.UtcNow
                    });
            }

            return Ok(new
            {
                status = "Healthy",
                database = "Connected",
                timestamp = DateTimeOffset.UtcNow
            });
        }
    }
}
