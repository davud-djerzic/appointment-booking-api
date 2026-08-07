using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace AppointmentBooking.Api.Exceptions
{
    public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested) return false;

            var problemDeatils = exception switch
            {
                NpgsqlException ex 
                when(ex.InnerException is System.Net.Sockets.SocketException) => 
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status503ServiceUnavailable,
                        Title = "Database unavailable",
                        Detail = "The database is currently unavailable. Please try again later.",
                        Instance = httpContext.Request.Path
                    },

                EmployeeEmailAlreadyExistsException =>
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Employee already exists",
                        Detail = exception.Message,
                        Instance = httpContext.Request.Path
                    },

                ServiceNameAlreadyExistsException =>
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Service already exists",
                        Detail = exception.Message,
                        Instance = httpContext.Request.Path
                    },

                NotFoundException ex => new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource not found",
                    Detail = exception.Message,
                    Instance = httpContext.Request.Path
                },

                ConflictException => new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = exception.Message,
                    Instance = httpContext.Request.Path
                },

                AppointmentSlotUnavailableException =>
                    new ProblemDetails
                    {
                        Status = StatusCodes.Status409Conflict,
                        Title = "Appointment slot unavailable",
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
