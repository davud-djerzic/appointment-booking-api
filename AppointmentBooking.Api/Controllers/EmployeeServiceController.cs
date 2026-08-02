using AppointmentBooking.Api.DTOs.EmployeeServices.Responses;
using AppointmentBooking.Api.Services.EmployeeServices;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/employees/{employeeId:long:min(1)}/services")]
    public sealed class EmployeeServiceController(IEmployeeServiceAssignmentService assignmentService) : ControllerBase
    {
        [HttpPost("{serviceId:long:min(1)}")]
        [ProducesResponseType<EmployeeServiceAssignmentResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<EmployeeServiceAssignmentResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Assign(long employeeId, long serviceId, CancellationToken cancellationToken)
        {
            AssignEmployeeServiceResult result = await assignmentService.AssignAsync(employeeId, serviceId, cancellationToken);

            return result.Status switch
            {
                AssignEmployeeServiceStatus.Assigned =>
                Created(
                    $"/api/employees/{employeeId}/services/{serviceId}",
                    result.Assignment),

                AssignEmployeeServiceStatus.Reactivated =>
                    Ok(result.Assignment),

                AssignEmployeeServiceStatus.EmployeeNotFound =>
                    NotFound(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Employee not found",
                        Detail = $"Employee with ID '{employeeId}' was not found.",
                        Instance = HttpContext.Request.Path
                    }),

                AssignEmployeeServiceStatus.ServiceNotFound =>
                   NotFound(new ProblemDetails
                   {
                       Status = StatusCodes.Status404NotFound,
                       Title = "Service not found",
                       Detail = $"Service with ID '{serviceId}' was not found.",
                       Instance = HttpContext.Request.Path
                   }),

                AssignEmployeeServiceStatus.Conflict =>
                    Conflict(new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Conflict",
                        Detail = result.Message,
                        Instance = HttpContext.Request.Path
                    }),

                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        [HttpDelete("{serviceId:long:min(1)}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(long employeeId, long serviceId, CancellationToken cancellationToken)
        {
            var result = await assignmentService.DeactivateAsync(employeeId, serviceId, cancellationToken);

            return result.Status switch
            {
                DeactivateEmployeeServiceStatus.Success =>
                    NoContent(),

                DeactivateEmployeeServiceStatus.EmployeeNotFound =>
                    NotFound(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Employee not found",
                        Detail = $"Employee with ID '{employeeId}' was not found.",
                        Instance = HttpContext.Request.Path
                    }),

                DeactivateEmployeeServiceStatus.ServiceNotFound =>
                    NotFound(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Service not found",
                        Detail = $"Service with ID '{serviceId}' was not found.",
                        Instance = HttpContext.Request.Path
                    }),

                DeactivateEmployeeServiceStatus.AssignmentNotFound =>
                   NotFound(new ProblemDetails
                   {
                       Status = StatusCodes.Status404NotFound,
                       Title = "Assignment not found",
                       Detail = $"Service '{serviceId}' is not assigned to employee '{employeeId}'.",
                       Instance = HttpContext.Request.Path
                   }),

                _ => StatusCode(StatusCodes.Status500InternalServerError)
            };
        }

        [HttpGet]
        [ProducesResponseType<IEnumerable<EmployeeServiceResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetServiceEmployees(long employeeId, CancellationToken cancellationToken)
        {
            var employees = await assignmentService.GetEmployeeServicesAsync(employeeId, cancellationToken);

            return Ok(employees);
        }

        [HttpGet("{serviceId:long:min(1)}")]
        [ProducesResponseType<EmployeeServiceAssignmentResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get(long employeeId, long serviceId, CancellationToken cancellationToken)
        { 
            var response = await assignmentService.GetServiceEmployeesAssignmentsAsync(employeeId, serviceId, cancellationToken);

            if (response is null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Assignment not found",
                    Detail = $"Service '{serviceId}' is not assigned to employee '{employeeId}'.",
                    Instance = HttpContext.Request.Path
                });
            }

            return Ok(response);
        }
    }
}
