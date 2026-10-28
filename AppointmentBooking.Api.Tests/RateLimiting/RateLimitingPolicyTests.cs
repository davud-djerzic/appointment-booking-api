using System.Net;
using AppointmentBooking.Api.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace AppointmentBooking.Api.Tests.RateLimiting
{
    /// <summary>
    /// Runs the real rate limiting configuration in an in-memory server with dummy endpoints,
    /// so the policies are tested without a database, Redis or Mailjet.
    /// </summary>
    public sealed class RateLimitingPolicyTests
    {
        [Fact]
        public async Task Login_Allows10RequestsPerMinute_ThenReturns429()
        {
            await using WebApplication app = await CreateAppAsync(clientIpHeader: "X-Real-IP");
            HttpClient client = app.GetTestClient();

            for (int i = 0; i < 10; i++)
            {
                (await PostAsync(client, "/login", "85.10.1.5")).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            HttpResponseMessage rejected = await PostAsync(client, "/login", "85.10.1.5");

            rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            rejected.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        }

        [Fact]
        public async Task Login_CountsEachClientSeparately()
        {
            await using WebApplication app = await CreateAppAsync(clientIpHeader: "X-Real-IP");
            HttpClient client = app.GetTestClient();

            for (int i = 0; i < 10; i++)
            {
                await PostAsync(client, "/login", "85.10.1.5");
            }

            (await PostAsync(client, "/login", "85.10.1.5")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            (await PostAsync(client, "/login", "91.22.7.8")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ForgotPassword_Allows3Requests_ThenReturns429()
        {
            await using WebApplication app = await CreateAppAsync(clientIpHeader: "X-Real-IP");
            HttpClient client = app.GetTestClient();

            for (int i = 0; i < 3; i++)
            {
                (await PostAsync(client, "/forgot-password", "85.10.1.5")).StatusCode.Should().Be(HttpStatusCode.OK);
            }

            (await PostAsync(client, "/forgot-password", "85.10.1.5")).StatusCode
                .Should().Be(HttpStatusCode.TooManyRequests);
        }

        [Fact]
        public async Task Register_ReturnsRetryAfter_WhenRejected()
        {
            await using WebApplication app = await CreateAppAsync(clientIpHeader: "X-Real-IP");
            HttpClient client = app.GetTestClient();

            for (int i = 0; i < 5; i++)
            {
                await PostAsync(client, "/register", "85.10.1.5");
            }

            HttpResponseMessage rejected = await PostAsync(client, "/register", "85.10.1.5");

            rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            rejected.Headers.RetryAfter.Should().NotBeNull();
        }

        [Fact]
        public async Task SpoofedHeader_IsIgnored_WhenNoTrustedHeaderIsConfigured()
        {
            await using WebApplication app = await CreateAppAsync(clientIpHeader: null);
            HttpClient client = app.GetTestClient();

            // A client inventing a new X-Real-IP on every request must still be limited.
            for (int i = 0; i < 10; i++)
            {
                await PostAsync(client, "/login", $"85.10.1.{i + 1}");
            }

            (await PostAsync(client, "/login", "85.10.1.200")).StatusCode
                .Should().Be(HttpStatusCode.TooManyRequests);
        }

        private static async Task<WebApplication> CreateAppAsync(string? clientIpHeader)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:ClientIpHeader"] = clientIpHeader
            });

            builder.Services.AddApiRateLimiting(builder.Configuration);

            WebApplication app = builder.Build();
            app.UseRateLimiter();

            app.MapPost("/login", () => Results.Ok()).RequireRateLimiting(RateLimitPolicies.Login);
            app.MapPost("/register", () => Results.Ok()).RequireRateLimiting(RateLimitPolicies.Register);
            app.MapPost("/forgot-password", () => Results.Ok()).RequireRateLimiting(RateLimitPolicies.ForgotPassword);

            await app.StartAsync();
            return app;
        }

        private static Task<HttpResponseMessage> PostAsync(HttpClient client, string path, string clientIp)
        {
            HttpRequestMessage request = new(HttpMethod.Post, path);
            request.Headers.Add("X-Real-IP", clientIp);
            return client.SendAsync(request);
        }
    }
}
