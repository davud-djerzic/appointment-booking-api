using AppointmentBooking.Api.DTOs.EmployeeServices.Responses;
using AppointmentBooking.Api.Services.EmployeeServices;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Controllers
{
    [ApiController]
    [Route("api/service/{serviceId:long:min(1)}/employees")]
    public sealed class ServiceEmployeesController(IEmployeeServiceAssignmentService assignmentService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<ServiceEmployeeResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetServiceEmployees(long serviceId, CancellationToken cancellationToken)
        {
            var services = await assignmentService.GetServiceEmployeesAsync(serviceId, cancellationToken);

            return Ok(services);
        }
    }
}
