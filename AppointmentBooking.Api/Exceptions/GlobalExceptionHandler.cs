using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentBooking.Api.Exceptions
{
    public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested) return false;

            var problemDeatils = exception switch
            {
                EmployeeEmailAlreadyExistsException =>
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Employee already exists",
                        Detail = exception.Message,
                        Instance = httpContext.Request.Path
                    },
                _ => new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal server error",
                    Detail = "An unexpected error occurred.",
                    Instance = httpContext.Request.Path
                }
            };

            if (problemDeatils.Status >= 500)
            {
                logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            } else
            {
                logger.LogWarning(exception, "Request failed with status code {StatusCode}", problemDeatils.Status);
            }

            problemDeatils.Extensions["traceId"] = httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = problemDeatils.Status ?? StatusCodes.Status500InternalServerError;

            await httpContext.Response.WriteAsJsonAsync(problemDeatils, cancellationToken);

            return true;
        }
    }
}
