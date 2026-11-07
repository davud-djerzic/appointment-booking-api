using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AppointmentBooking.Api.RateLimiting
{
    /// <summary>
    /// Runs the action only when PasswordHashingLimiter has a free slot; otherwise 429.
    /// It is an action filter, so it runs after the per-IP rate limits and after model
    /// validation: rejected or invalid requests never take a slot.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class LimitPasswordHashingAttribute : Attribute, IAsyncActionFilter
    {
        // Overload lasts only while many requests arrive at once; the app can retry soon.
        private const int RetryAfterSeconds = 5;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            HttpContext httpContext = context.HttpContext;
            PasswordHashingLimiter limiter = httpContext.RequestServices.GetRequiredService<PasswordHashingLimiter>();

            using RateLimitLease lease = await limiter.AcquireAsync(httpContext.RequestAborted);

            if (!lease.IsAcquired)
            {
                httpContext.RequestServices
                    .GetRequiredService<ILogger<LimitPasswordHashingAttribute>>()
                    .LogWarning(
                        "Too many simultaneous password requests, rejected: {Method} {Path}",
                        httpContext.Request.Method,
                        httpContext.Request.Path);

                httpContext.Response.Headers.RetryAfter = RetryAfterSeconds.ToString();

                context.Result = new ObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "Too many requests. Please try again later.",
                    Instance = httpContext.Request.Path
                })
                {
                    StatusCode = StatusCodes.Status429TooManyRequests,
                    ContentTypes = { "application/problem+json" }
                };

                return;
            }

            await next();
        }
    }
}
