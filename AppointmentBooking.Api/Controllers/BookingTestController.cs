using AppointmentBooking.Api.Repositories.Bookings;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/booking-test")]
    public sealed class BookingTestController(
       IBookingRepository bookingRepository)
       : ControllerBase
    {
        [HttpGet("overlap")]
        public async Task<ActionResult<bool>> HasOverlap(
            [FromQuery] long employeeId,
            [FromQuery] DateTimeOffset startsAt,
            [FromQuery] DateTimeOffset endsAt,
            CancellationToken cancellationToken)
        {
            bool hasOverlap =
                await bookingRepository.HasScheduledOverlapAsync(
                    employeeId,
                    startsAt,
                    endsAt,
                    cancellationToken);

            return Ok(hasOverlap);
        }
    }
}
