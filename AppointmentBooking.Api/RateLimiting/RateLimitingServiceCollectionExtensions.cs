using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AppointmentBooking.Api.RateLimiting
{
    /// <summary>
    /// Rate limiting for production.
    /// Counters live in the API process memory (one instance on Railway): no Redis or database calls,
    /// negligible memory, and rejected requests never reach the database, password hashing or Mailjet.
    /// </summary>
    public static class RateLimitingServiceCollectionExtensions
    {
        private const string RejectedLoggerCategory = "AppointmentBooking.Api.RateLimiting";

        public static IServiceCollection AddApiRateLimiting(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            RateLimitingOptions rateLimitingOptions =
                configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
                ?? new RateLimitingOptions();

            string? clientIpHeader = rateLimitingOptions.ClientIpHeader;

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Safety net for every endpoint: 120 requests per minute per client.
                // App start sends ~10-15 requests, so real users never get close.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ClientIpResolver.GetPartitionKey(context, clientIpHeader),
                        _ => FixedWindow(permitLimit: 120, window: TimeSpan.FromMinutes(1))));

                // Password guessing: 10 attempts per minute per client (sliding, no burst at window edges).
                options.AddPolicy(RateLimitPolicies.Login, context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        ClientIpResolver.GetPartitionKey(context, clientIpHeader),
                        _ => SlidingWindow(permitLimit: 10, window: TimeSpan.FromMinutes(1), segments: 6)));

                // Fake accounts: 5 registrations per hour per client.
                options.AddPolicy(RateLimitPolicies.Register, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ClientIpResolver.GetPartitionKey(context, clientIpHeader),
                        _ => FixedWindow(permitLimit: 5, window: TimeSpan.FromHours(1))));

                // Token refresh: 30 per minute per client.
                options.AddPolicy(RateLimitPolicies.Refresh, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ClientIpResolver.GetPartitionKey(context, clientIpHeader),
                        _ => FixedWindow(permitLimit: 30, window: TimeSpan.FromMinutes(1))));

                // Every request sends an email through Mailjet: 3 per 15 minutes per client.
                options.AddPolicy(RateLimitPolicies.ForgotPassword, context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        ClientIpResolver.GetPartitionKey(context, clientIpHeader),
                        _ => SlidingWindow(permitLimit: 3, window: TimeSpan.FromMinutes(15), segments: 3)));

                // Reset code guessing: 10 per 15 minutes per client (codes also expire after 5 wrong attempts).
                options.AddPolicy(RateLimitPolicies.ResetCode, context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        ClientIpResolver.GetPartitionKey(context, clientIpHeader),
                        _ => SlidingWindow(permitLimit: 10, window: TimeSpan.FromMinutes(15), segments: 3)));

                // Blocking free slots with holds: 20 per minute per signed-in customer.
                options.AddPolicy(RateLimitPolicies.BookingHold, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetUserPartitionKey(context, clientIpHeader),
                        _ => FixedWindow(permitLimit: 20, window: TimeSpan.FromMinutes(1))));

                options.OnRejected = async (rejectedContext, cancellationToken) =>
                {
                    HttpContext httpContext = rejectedContext.HttpContext;

                    if (rejectedContext.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                    {
                        httpContext.Response.Headers.RetryAfter =
                            Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
                                .ToString(CultureInfo.InvariantCulture);
                    }

                    // Log only the route template (no ids or tokens from the URL), the policy
                    // and a masked client address - no emails, passwords or full IP addresses.
                    Endpoint? endpoint = httpContext.GetEndpoint();
                    string route = (endpoint as RouteEndpoint)?.RoutePattern.RawText ?? "unknown";
                    string policy = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
                        ?? "global";

                    httpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger(RejectedLoggerCategory)
                        .LogWarning(
                            "Rate limit exceeded. Policy: {Policy}, endpoint: {Method} {Route}, client: {MaskedClient}",
                            policy,
                            httpContext.Request.Method,
                            route,
                            ClientIpResolver.GetMaskedAddress(httpContext, clientIpHeader));

                    await httpContext.Response.WriteAsJsonAsync(
                        new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Too many requests",
                            Detail = "Too many requests. Please try again later.",
                            Instance = httpContext.Request.Path
                        },
                        options: (System.Text.Json.JsonSerializerOptions?)null,
                        contentType: "application/problem+json",
                        cancellationToken: cancellationToken);
                };
            });

            return services;
        }

        private static string GetUserPartitionKey(HttpContext context, string? clientIpHeader)
        {
            string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(userId)
                ? "ip:" + ClientIpResolver.GetPartitionKey(context, clientIpHeader)
                : "user:" + userId;
        }

        // QueueLimit = 0: rejected requests are answered immediately instead of waiting in memory.
        private static FixedWindowRateLimiterOptions FixedWindow(int permitLimit, TimeSpan window) => new()
        {
            PermitLimit = permitLimit,
            Window = window,
            QueueLimit = 0,
            AutoReplenishment = true
        };

        private static SlidingWindowRateLimiterOptions SlidingWindow(int permitLimit, TimeSpan window, int segments) => new()
        {
            PermitLimit = permitLimit,
            Window = window,
            SegmentsPerWindow = segments,
            QueueLimit = 0,
            AutoReplenishment = true
        };
    }
}
