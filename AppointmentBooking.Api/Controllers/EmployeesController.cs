using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.DTOs.Employees.Request;
using AppointmentBooking.Api.DTOs.Employees.Response;
using AppointmentBooking.Api.Services.Employees;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class EmployeesController(IEmployeeService employeeService) : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType<EmployeeResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployeeResponse>> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
        {
            var createdEmployee = await employeeService.CreateAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = createdEmployee.Id}, createdEmployee);
        }

        [HttpGet("{id:long}")]
        [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EmployeeResponse>> GetById(long id, CancellationToken cancellationToken)
        {
            var employee = await employeeService.GetByIdAsync(id, cancellationToken);
            if (employee is null)
            {
                return NotFound(new ProblemDetails
                {

                    Status = StatusCodes.Status404NotFound,
                    Title = "Employee not found",
                    Detail = $"Employee with ID '{id}' was not found.",
                    Instance = HttpContext.Request.Path
                });
            }
            return Ok(employee);
        }

        [HttpDelete("{id:long:min(1)}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            var deactivated = await employeeService.DeactivateAsync(id, cancellationToken);

            if (!deactivated) {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Employee not found",
                    Detail = $"Employee with ID '{id}' was not found.",
                    Instance = HttpContext.Request.Path
                });
            }

            return NoContent();
        }

        [HttpGet]
        [ProducesResponseType<PagedResponse<EmployeeResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResponse<EmployeeResponse>>> GetAll([FromQuery] GetEmployeesQuery query, CancellationToken cancellationToken)
        {
            var result = await employeeService.GetAllAsync(query, cancellationToken);
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
            var updatedEmployee = await employeeService.UpdateAsync(id, request, cancellationToken);
            if (updatedEmployee is null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Employee not found",
                    Detail = $"Employee with ID '{id}' was not found.",
                    Instance = HttpContext.Request.Path
                });
            }
            return Ok(updatedEmployee);
        }

        [HttpPost("{id:long:min(1)}/activate")]
        [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployeeResponse>> Activate(long id, CancellationToken cancellationToken)
        {
            EmployeeResponse? employee = await employeeService.ActivateAsync(id, cancellationToken);

            if (employee is null) return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Employee not found",
                Detail = $"Employee with ID '{id}' was not found.",
                Instance = HttpContext.Request.Path
            });

            return Ok(employee);
        }
    }
}
