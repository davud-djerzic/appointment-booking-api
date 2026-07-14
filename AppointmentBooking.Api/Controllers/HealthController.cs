

using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "Healthy",
                application = "Appointment Booking API",
                timestamp = DateTimeOffset.UtcNow
            });
        }
    }
}
