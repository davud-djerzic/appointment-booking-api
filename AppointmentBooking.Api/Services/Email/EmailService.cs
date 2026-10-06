using AppointmentBooking.Api.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AppointmentBooking.Api.Services.Email
{
    public sealed class MailjetEmailService(HttpClient httpClient, IOptions<MailjetOptions> options, ILogger<MailjetEmailService> logger) : IEmailService
    {
        private readonly MailjetOptions mailjetOptions = options.Value;
        public async Task SendAsync(string toEmail, string? toName, string subject, string textPart, string htmlPart, CancellationToken cancellationToken)
        {
            var payload = new
            {
                Messages = new[]
            {
                new
                {
                    From = new
                    {
                        Email = mailjetOptions.FromEmail,
                        Name = mailjetOptions.FromName
                    },
                    To = new[]
                    {
                        new
                        {
                            Email = toEmail,
                            Name = toName ?? string.Empty
                        }
                    },
                    Subject = subject,
                    TextPart = textPart,
                    HTMLPart = htmlPart
                }
            }
            };

            string json = JsonSerializer.Serialize(payload);

            using HttpRequestMessage request =
                new(HttpMethod.Post, "v3.1/send");

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            string credentials =
                $"{mailjetOptions.ApiKey}:{mailjetOptions.SecretKey}";

            string base64Credentials =
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(credentials));

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Basic",
                    base64Credentials);

            using HttpResponseMessage response =
                await httpClient.SendAsync(
                    request,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string responseBody =
                    await response.Content.ReadAsStringAsync(
                        cancellationToken);

                logger.LogError(
                    "Mailjet email sending failed. StatusCode: {StatusCode}, Response: {Response}",
                    (int)response.StatusCode,
                    responseBody);

                throw new HttpRequestException(
                    $"Mailjet email sending failed with status code {(int)response.StatusCode}.");
            }
        }
    }
}
