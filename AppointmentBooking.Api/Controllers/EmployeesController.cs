using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.DTOs.Employees.Request;
using AppointmentBooking.Api.DTOs.Employees.Response;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.Services.Employees;
using AppointmentBooking.Api.Services.EmployeeWorkingHoursService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class EmployeesController(IEmployeeService employeeService) : ControllerBase
    {
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ProducesResponseType<EmployeeResponse>( StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployeeResponse>> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
        {
            EmployeeResponse createdEmployee = await employeeService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdEmployee.Id },
                createdEmployee);
        }

        [HttpGet("{id:long:min(1)}")]
        [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeResponse>> GetById(long id, CancellationToken cancellationToken)
        {
            EmployeeResponse employee = await employeeService.GetByIdAsync(id, cancellationToken);

            return Ok(employee);
        }

        [HttpGet]
        [ProducesResponseType<PagedResponse<EmployeeResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResponse<EmployeeResponse>>> GetAll([FromQuery] GetEmployeesQuery query, CancellationToken cancellationToken)
        {
            PagedResponse<EmployeeResponse> result = await employeeService.GetAllAsync(query, cancellationToken);

            return Ok(result);
        }

        [HttpPut("{id:long:min(1)}")]
        [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployeeResponse>> Update(long id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
        {
            EmployeeResponse updatedEmployee =await employeeService.UpdateAsync(id, request, cancellationToken);

            return Ok(updatedEmployee);
        }

        [HttpDelete("{id:long:min(1)}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await employeeService.DeactivateAsync(id, cancellationToken);

            return NoContent();
        }

        [HttpPost("{id:long:min(1)}/activate")]
        [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>( StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployeeResponse>> Activate(long id, CancellationToken cancellationToken)
        {
            EmployeeResponse employee =await employeeService.ActivateAsync(id, cancellationToken);

            return Ok(employee);
        }

        [Authorize(Roles = "Employee")]
        [HttpGet("me")]
        [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeResponse>> GetMyProfile(CancellationToken cancellationToken)
        {
            EmployeeResponse employee = await employeeService.GetMyProfileAsync(cancellationToken);

            return Ok(employee);
        }
    }
}
