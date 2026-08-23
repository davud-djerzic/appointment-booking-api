using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Request;
using AppointmentBooking.Api.Models;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AppointmentBooking.Api.Tests.DTOs.EmployeeWorkingHours
{
    public sealed class EmployeeWorkingHoursRequestValidationTests
    {
        private static List<ValidationResult> ValidateModel(object model)
        {
            ValidationContext context = new(model);

            List<ValidationResult> results = [];

            Validator.TryValidateObject(
                model,
                context,
                results,
                validateAllProperties: true);

            return results;
        }

        [Fact]
        public void CreateRequest_ShouldBeValid_WhenRequestIsValid()
        {
            CreateEmployeeWorkingHoursRequest request = new()
            {
                DayOfWeek = WeekDay.Monday,
                StartsAt = new TimeOnly(9, 0),
                EndsAt = new TimeOnly(17, 0)
            };

            List<ValidationResult> results = ValidateModel(request);

            results.Count.Should().Be(0);
        }

        [Fact]
        public void CreateRequest_ShouldBeInvalid_WhenDayOfWeekIsInvalid()
        {
            CreateEmployeeWorkingHoursRequest request = new()
            {
                DayOfWeek = (WeekDay)0,
                StartsAt = new TimeOnly(9, 0),
                EndsAt = new TimeOnly(17, 0)
            };

            List<ValidationResult> results = ValidateModel(request);

            results.Should().ContainSingle(x =>
                x.ErrorMessage ==
                "DayOfWeek must be a valid weekday.");
        }

        [Fact]
        public void CreateRequest_ShouldBeInvalid_WhenDayOfWeekIsGreaterThanSeven()
        {
            CreateEmployeeWorkingHoursRequest request = new()
            {
                DayOfWeek = (WeekDay)8,
                StartsAt = new TimeOnly(9, 0),
                EndsAt = new TimeOnly(17, 0)
            };

            List<ValidationResult> results = ValidateModel(request);

            results.Should().ContainSingle(x =>
                x.ErrorMessage ==
                "DayOfWeek must be a valid weekday.");
        }

        [Fact]
        public void CreateRequest_ShouldBeInvalid_WhenStartAndEndTimesAreEqual()
        {
            CreateEmployeeWorkingHoursRequest request = new()
            {
                DayOfWeek = WeekDay.Monday,
                StartsAt = new TimeOnly(9, 0),
                EndsAt = new TimeOnly(9, 0)
            };

            List<ValidationResult> results = ValidateModel(request);

            results.Should().ContainSingle(x =>
                x.ErrorMessage ==
                "EndsAt must be later than StartsAt.");
        }

        [Fact]
        public void CreateRequest_ShouldBeInvalid_WhenEndTimeIsBeforeStartTime()
        {
            CreateEmployeeWorkingHoursRequest request = new()
            {
                DayOfWeek = WeekDay.Monday,
                StartsAt = new TimeOnly(17, 0),
                EndsAt = new TimeOnly(9, 0)
            };

            List<ValidationResult> results = ValidateModel(request);

            results.Should().ContainSingle(x =>
                x.ErrorMessage ==
                "EndsAt must be later than StartsAt.");
        }

        [Fact]
        public void UpdateRequest_ShouldBeValid_WhenRequestIsValid()
        {
            UpdateEmployeeWorkingHoursRequest request = new()
            {
                DayOfWeek = WeekDay.Tuesday,
                StartsAt = new TimeOnly(10, 0),
                EndsAt = new TimeOnly(18, 0)
            };

            List<ValidationResult> results = ValidateModel(request);

            results.Count.Should().Be(0);
        }

        [Fact]
        public void UpdateRequest_ShouldBeInvalid_WhenDayOfWeekIsInvalid()
        {
            UpdateEmployeeWorkingHoursRequest request = new()
            {
                DayOfWeek = (WeekDay)0,
                StartsAt = new TimeOnly(10, 0),
                EndsAt = new TimeOnly(18, 0)
            };

            List<ValidationResult> results = ValidateModel(request);

            results.Should().ContainSingle(x =>
                x.ErrorMessage ==
                "DayOfWeek must be a valid weekday.");
        }

        [Fact]
        public void UpdateRequest_ShouldBeInvalid_WhenEndTimeIsNotLaterThanStartTime()
        {
            UpdateEmployeeWorkingHoursRequest request = new()
            {
                DayOfWeek = WeekDay.Tuesday,
                StartsAt = new TimeOnly(18, 0),
                EndsAt = new TimeOnly(18, 0)
            };

            List<ValidationResult> results = ValidateModel(request);

            results.Should().ContainSingle(x =>
                x.ErrorMessage ==
                "EndsAt must be later than StartsAt.");
        }
    }
}
