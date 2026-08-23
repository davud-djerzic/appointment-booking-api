using AppointmentBooking.Api.DTOs.Common;
using AppointmentBooking.Api.Repositories.Appointments;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppointmentBooking.Api.Tests.Integration
{
    public sealed class AppointmentsApiTests : IClassFixture<IntegrationTestWebApplicationFactory>
    {
        private readonly HttpClient client;

        public AppointmentsApiTests(IntegrationTestWebApplicationFactory factory)
        {
            client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAppointments_ShouldReturnSeededAppointments()
        {
            HttpResponseMessage response = await client.GetAsync("/api/Appointments");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            string content = await response.Content.ReadAsStringAsync();

            content.Should().NotBeNullOrWhiteSpace();

            JsonSerializerOptions jsonOptions = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            jsonOptions.Converters.Add(new JsonStringEnumConverter());

            PagedResponse<AppointmentListItem>? result =
                await response.Content
                    .ReadFromJsonAsync<PagedResponse<AppointmentListItem>>(jsonOptions);

            result.Should().NotBeNull();
            result!.Items.Should().NotBeNull();
        }
    }
}
