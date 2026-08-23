using AppointmentBooking.Api.DTOs.EmployeeWorkingHours.Request;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace AppointmentBooking.Api.OpenApi
{
    public sealed class EmployeeWorkingHoursSchemaTransformer : IOpenApiSchemaTransformer
    {
        public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
        {
            if (context.JsonTypeInfo.Type == typeof(CreateEmployeeWorkingHoursRequest) || context.JsonTypeInfo.Type == typeof(UpdateEmployeeWorkingHoursRequest))
            {
                schema.Example = new JsonObject
                {
                    ["dayOfWeek"] = "Monday",
                    ["startsAt"] = "09:00:00",
                    ["endsAt"] = "17:00:00"
                };
            }

            return Task.CompletedTask;
        }
    }
}
