using AppointmentBooking.Api.DTOs.Employees.Request;
using AppointmentBooking.Api.DTOs.Employees.Response;
using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Request;
using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Response;
using AppointmentBooking.Api.Models;
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
    public sealed class EmployeeWorkingHoursApiTests : IClassFixture<IntegrationTestWebApplicationFactory>
    {
        private readonly HttpClient client;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        public EmployeeWorkingHoursApiTests(IntegrationTestWebApplicationFactory factory)
        {
            client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateWorkingHours_ShouldReturnConflict_WhenWorkingHoursOverlap()
        {
            CreateEmployeeRequest employeeRequest = new()
            {
                FirstName = "Integration",
                LastName = "Test",
                Email = $"integration-{Guid.NewGuid()}@test.com",
                Phone = "123456789"
            };

            HttpResponseMessage employeeResponse =
                await client.PostAsJsonAsync(
                    "/api/Employees",
                    employeeRequest,
                    jsonOptions);

            employeeResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.Created);

            EmployeeResponse? employee =
                await employeeResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(
                        jsonOptions);

            employee.Should().NotBeNull();

            long employeeId = employee!.Id;

            long? workingHoursId = null;

            try
            {
                CreateEmployeeWorkingHoursRequest firstRequest = new()
                {
                    DayOfWeek = WeekDay.Monday,
                    StartsAt = new TimeOnly(9, 0),
                    EndsAt = new TimeOnly(12, 0)
                };

                CreateEmployeeWorkingHoursRequest overlappingRequest = new()
                {
                    DayOfWeek = WeekDay.Monday,
                    StartsAt = new TimeOnly(11, 0),
                    EndsAt = new TimeOnly(15, 0)
                };

                HttpResponseMessage firstResponse =
                    await client.PostAsJsonAsync(
                        $"/api/Employees/{employeeId}/working-hours",
                        firstRequest,
                        jsonOptions);

                firstResponse.StatusCode
                    .Should()
                    .Be(HttpStatusCode.Created);

                EmployeeWorkingHoursResponse? firstWorkingHours =
                    await firstResponse.Content
                        .ReadFromJsonAsync<EmployeeWorkingHoursResponse>(
                            jsonOptions);

                firstWorkingHours.Should().NotBeNull();

                workingHoursId = firstWorkingHours!.Id;

                HttpResponseMessage secondResponse =
                    await client.PostAsJsonAsync(
                        $"/api/Employees/{employeeId}/working-hours",
                        overlappingRequest,
                        jsonOptions);

                secondResponse.StatusCode
                    .Should()
                    .Be(HttpStatusCode.Conflict);
            }
            finally
            {
                HttpResponseMessage deactivateEmployeeResponse =
                    await client.DeleteAsync(
                        $"/api/Employees/{employeeId}");

                deactivateEmployeeResponse.StatusCode
                    .Should()
                    .Be(HttpStatusCode.NoContent);
            }
        }

        [Fact]
        public async Task CreateWorkingHours_ShouldAllowSameTime_WhenDaysAreDifferent()
        {
            CreateEmployeeRequest employeeRequest = new()
            {
                FirstName = "Integration",
                LastName = "Test",
                Email = $"integration-{Guid.NewGuid()}@test.com",
                Phone = "123456789"
            };

            HttpResponseMessage employeeResponse =
                await client.PostAsJsonAsync(
                    "/api/Employees",
                    employeeRequest,
                    jsonOptions);

            employeeResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.Created);

            EmployeeResponse? employee =
                await employeeResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(jsonOptions);

            employee.Should().NotBeNull();

            long employeeId = employee!.Id;

            List<long> workingHoursIds = [];

            try
            {
                CreateEmployeeWorkingHoursRequest mondayRequest = new()
                {
                    DayOfWeek = WeekDay.Monday,
                    StartsAt = new TimeOnly(9, 0),
                    EndsAt = new TimeOnly(12, 0)
                };

                CreateEmployeeWorkingHoursRequest tuesdayRequest = new()
                {
                    DayOfWeek = WeekDay.Tuesday,
                    StartsAt = new TimeOnly(9, 0),
                    EndsAt = new TimeOnly(12, 0)
                };

                HttpResponseMessage mondayResponse =
                    await client.PostAsJsonAsync(
                        $"/api/Employees/{employeeId}/working-hours",
                        mondayRequest,
                        jsonOptions);

                mondayResponse.StatusCode
                    .Should()
                    .Be(HttpStatusCode.Created);

                EmployeeWorkingHoursResponse? mondayWorkingHours =
                    await mondayResponse.Content
                        .ReadFromJsonAsync<EmployeeWorkingHoursResponse>(
                            jsonOptions);

                mondayWorkingHours.Should().NotBeNull();
                workingHoursIds.Add(mondayWorkingHours!.Id);

                HttpResponseMessage tuesdayResponse =
                    await client.PostAsJsonAsync(
                        $"/api/Employees/{employeeId}/working-hours",
                        tuesdayRequest,
                        jsonOptions);

                tuesdayResponse.StatusCode
                    .Should()
                    .Be(HttpStatusCode.Created);

                EmployeeWorkingHoursResponse? tuesdayWorkingHours =
                    await tuesdayResponse.Content
                        .ReadFromJsonAsync<EmployeeWorkingHoursResponse>(
                            jsonOptions);

                tuesdayWorkingHours.Should().NotBeNull();
                workingHoursIds.Add(tuesdayWorkingHours!.Id);
            }
            finally
            {
                // Deactivating the employee removes all of his working hours.
                HttpResponseMessage deactivateResponse =
                    await client.DeleteAsync(
                        $"/api/Employees/{employeeId}");

                deactivateResponse.StatusCode
                    .Should()
                    .Be(HttpStatusCode.NoContent);
            }
        }

        [Fact]
        public async Task CreateWorkingHours_ShouldAllowAdjacentWorkingHours()
        {
            CreateEmployeeRequest employeeRequest = new()
            {
                FirstName = "Integration",
                LastName = "Test",
                Email = $"integration-{Guid.NewGuid()}@test.com",
                Phone = "123456789"
            };

            HttpResponseMessage employeeResponse =
                await client.PostAsJsonAsync(
                    "/api/Employees",
                    employeeRequest,
                    jsonOptions);

            employeeResponse.StatusCode
                .Should()
                .Be(HttpStatusCode.Created);

            EmployeeResponse? employee =
                await employeeResponse.Content
                    .ReadFromJsonAsync<EmployeeResponse>(jsonOptions);

            employee.Should().NotBeNull();

            long employeeId = employee!.Id;

            try
            {
                CreateEmployeeWorkingHoursRequest firstRequest = new()
                {
                    DayOfWeek = WeekDay.Monday,
                    StartsAt = new TimeOnly(9, 0),
                    EndsAt = new TimeOnly(12, 0)
                };

                CreateEmployeeWorkingHoursRequest adjacentRequest = new()
                {
                    DayOfWeek = WeekDay.Monday,
                    StartsAt = new TimeOnly(12, 0),
                    EndsAt = new TimeOnly(15, 0)
                };

                HttpResponseMessage firstResponse =
                    await client.PostAsJsonAsync(
                        $"/api/Employees/{employeeId}/working-hours",
                        firstRequest,
                        jsonOptions);

                firstResponse.StatusCode
                    .Should()
                    .Be(HttpStatusCode.Created);

                HttpResponseMessage secondResponse =
                    await client.PostAsJsonAsync(
                        $"/api/Employees/{employeeId}/working-hours",
                        adjacentRequest,
                        jsonOptions);

                secondResponse.StatusCode
                    .Should()
                    .Be(HttpStatusCode.Created);
            }
            finally
            {
                HttpResponseMessage deactivateResponse =
                    await client.DeleteAsync(
                        $"/api/Employees/{employeeId}");

                deactivateResponse.StatusCode
                    .Should()
                    .Be(HttpStatusCode.NoContent);
            }
        }
    }
}
