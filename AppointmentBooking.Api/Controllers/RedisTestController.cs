using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Repositories.BookingHolds;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/redis-test")]
    public sealed class RedisTestController(
       IBookingHoldRepository bookingHoldRepository)
       : ControllerBase
    {
        [HttpPost("holds")]
        public async Task<IActionResult> Create(
            BookingHold hold,
            CancellationToken cancellationToken)
        {
            bool created =
                await bookingHoldRepository.TryCreateAsync(
                    hold,
                    TimeSpan.FromMinutes(5),
                    cancellationToken);

            if (!created)
            {
                return Conflict(new
                {
                    message = "The requested time overlaps an existing hold."
                });
            }

            return Ok(hold);
        }

        [HttpGet("holds/{holdToken:guid}")]
        public async Task<IActionResult> Get(
            Guid holdToken,
            CancellationToken cancellationToken)
        {
            BookingHold? hold =
                await bookingHoldRepository.GetAsync(
                    holdToken,
                    cancellationToken);

            return hold is null
                ? NotFound()
                : Ok(hold);
        }

        [HttpDelete("holds/{holdToken:guid}")]
        public async Task<IActionResult> Delete(
            Guid holdToken,
            CancellationToken cancellationToken)
        {
            BookingHold? hold =
                await bookingHoldRepository.GetAsync(
                    holdToken,
                    cancellationToken);

            if (hold is null)
            {
                return NotFound();
            }

            await bookingHoldRepository.DeleteAsync(
                hold,
                cancellationToken);

            return NoContent();
        }
    }
}
