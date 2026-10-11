using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Request;
using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Response;
using AppointmentBooking.Api.Services.EmployeeWorkingHoursService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers;

[ApiController]
[Authorize(Roles = "Employee, Admin")]
[Route("api/employees/me/working-hours")]
public sealed class MyEmployeeWorkingHoursController(
    IEmployeeRecurringBreakService recurringBreakService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<EmployeeWorkingHoursResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<EmployeeWorkingHoursResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<EmployeeWorkingHoursResponse> response =
            await recurringBreakService.GetForCurrentEmployeeAsync(cancellationToken);

        return Ok(response);
    }

    [HttpPut("recurring-break")]
    [ProducesResponseType<IReadOnlyCollection<EmployeeWorkingHoursResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IReadOnlyCollection<EmployeeWorkingHoursResponse>>> ApplyRecurringBreak(
        ApplyRecurringBreakRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<EmployeeWorkingHoursResponse> response =
            await recurringBreakService.ApplyForCurrentEmployeeAsync(request, cancellationToken);

        return Ok(response);
    }
}
