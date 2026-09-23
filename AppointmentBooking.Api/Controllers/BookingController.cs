using AppointmentBooking.Api.DTOs.BookingHolds.Response;
using AppointmentBooking.Api.DTOs.Bookings.Request;
using AppointmentBooking.Api.DTOs.Bookings.Response;
using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.Services.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class BookingsController(
        IBookingService bookingService)
        : ControllerBase
    {
        [Authorize(Roles = "Customer")]
        [HttpPost("hold")]
        [ProducesResponseType<BookingHoldResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<BookingHoldResponse>> Create(CreateBookingRequest request, CancellationToken cancellationToken)
        {
            BookingHoldResponse createdHoldBooking = await bookingService.CreateHoldAsync(request, cancellationToken);

            return StatusCode(StatusCodes.Status201Created, createdHoldBooking);
        }

        [Authorize(Roles = "Customer")]
        [HttpPost("{holdToken:guid}/confirm")]
        [ProducesResponseType<BookingResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<BookingResponse>> Confirm(Guid holdToken, CancellationToken cancellationToken)
        {
            BookingResponse booking = await bookingService.ConfirmAsync(holdToken,cancellationToken);

            return StatusCode(StatusCodes.Status201Created, booking);
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("{id:long:min(1)}")]
        [ProducesResponseType<BookingDetailsResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingDetailsResponse>> GetById(long id, CancellationToken cancellationToken)
        {
            BookingDetailsResponse booking = await bookingService.GetByIdAsync(id, cancellationToken);
                    
            return Ok(booking);
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("my")]
        [ProducesResponseType<PagedResponse<BookingSummaryResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResponse<BookingSummaryResponse>>> GetMyBookings([FromQuery] GetBookingsQuery query,CancellationToken cancellationToken)
        {
            PagedResponse<BookingSummaryResponse> result = await bookingService.GetMyBookingsAsync(query, cancellationToken);

            return Ok(result);
        }

        [Authorize(Roles = "Customer")]
        [HttpDelete("hold/{holdToken:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CancelHold(Guid holdToken,CancellationToken cancellationToken)
        {
            await bookingService.CancelHoldAsync(holdToken, cancellationToken);

            return NoContent();
        }

        [Authorize(Roles = "Customer,Employee,Admin")]
        [HttpPost("{id:long:min(1)}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CancelBooking(long id,CancellationToken cancellationToken)
        {
            await bookingService.CancelBookingAsync(id, cancellationToken);
               
            return NoContent();
        }

        [Authorize(Roles = "Employee,Admin")]
        [HttpPost("{id:long:min(1)}/complete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CompleteBooking(long id, CancellationToken cancellationToken)
        {
            await bookingService.CompleteBookingAsync(id, cancellationToken);

            return NoContent();
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("employee")]
        [ProducesResponseType<PagedResponse<EmployeeBookingSummaryResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResponse<EmployeeBookingSummaryResponse>>> GetEmployeeBookings([FromQuery] GetBookingsQuery query, CancellationToken cancellationToken)
        {
            PagedResponse<EmployeeBookingSummaryResponse> result = await bookingService.GetEmployeeBookingsAsync(query,cancellationToken);

            return Ok(result);
        }

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType<PagedResponse<AdminBookingSummaryResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PagedResponse<AdminBookingSummaryResponse>>> GetAdminBookingsAsync([FromQuery] GetAdminBookingsQuery query, CancellationToken cancellationToken)
        {
            PagedResponse<AdminBookingSummaryResponse> result = await bookingService.GetAdminBookingsAsync(query, cancellationToken);

            return Ok(result);
        }

        [Authorize]
        [HttpGet("availability")]
        [ProducesResponseType<BookingAvailabilityResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BookingAvailabilityResponse>> GetAvailabilityAsync([FromQuery] GetBookingAvailabilityQuery query,CancellationToken cancellationToken)
        {
            BookingAvailabilityResponse result = await bookingService.GetAvailabilityAsync(query, cancellationToken);

            return Ok(result);
        }

        [HttpGet("quick-availability")]
        [ProducesResponseType<QuickAvailabilityResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<QuickAvailabilityResponse>> GetQuickAvailability([FromQuery] GetQuickAvailabilityQuery query, CancellationToken cancellationToken)
        {
            QuickAvailabilityResponse response = await bookingService.GetQuickAvailabilityAsync(query, cancellationToken);

            return Ok(response);
        }

        [Authorize(Roles = "Customer")]
        [HttpGet("bookable-employees")]
        [ProducesResponseType<IReadOnlyList<BookableEmployeeResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<BookableEmployeeResponse>>> GetBookableEmployees([FromQuery] long[] serviceIds,CancellationToken cancellationToken)
        {
            IReadOnlyList<BookableEmployeeResponse> employees =
                await bookingService.GetBookableEmployeesAsync(
                    serviceIds,
                    cancellationToken);

            return Ok(employees);
        }
    }
}
