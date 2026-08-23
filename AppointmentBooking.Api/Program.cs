using AppointmentBooking.Api.BackgroundServices;
using AppointmentBooking.Api.Configuration;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.OpenApi;
using AppointmentBooking.Api.Repositories;
using AppointmentBooking.Api.Repositories.Appointments;
using AppointmentBooking.Api.Repositories.Employees;
using AppointmentBooking.Api.Repositories.EmployeeServices;
using AppointmentBooking.Api.Repositories.EmployeeWorkingHoursRepository;
using AppointmentBooking.Api.Repositories.Services;
using AppointmentBooking.Api.Services.Appointments;
using AppointmentBooking.Api.Services.BookedServices;
using AppointmentBooking.Api.Services.Employees;
using AppointmentBooking.Api.Services.EmployeeServices;
using AppointmentBooking.Api.Services.EmployeeWorkingHoursService;
using Npgsql;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer<
        EmployeeWorkingHoursSchemaTransformer>();
});

var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? throw new InvalidOperationException("Database connection string is missing");

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSingleton<NpgsqlDataSource>(_ =>
    NpgsqlDataSource.Create(connectionString));

builder.Services.Configure<AppointmentOptions>(builder.Configuration.GetSection(AppointmentOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<ExpiredAppointmnetCleanupService>();
builder.Services.AddHostedService<AutomaticAppointmentCompletionService>();

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IBookableServiceRepository, BookableServiceRepository>();
builder.Services.AddScoped<IBookableServiceService, BookableServiceService>();
builder.Services.AddScoped<IEmployeeServiceRepository, EmployeeServiceRepository>();
builder.Services.AddScoped<IEmployeeServiceAssignmentService, EmployeeServiceAssignmentService>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IEmployeeWorkingHoursRepository, EmployeeWorkingHoursRepository>();
builder.Services.AddScoped<IEmployeeWorkingHoursService, EmployeeWorkingHoursService>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors("Frontend");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "Appointment Booking API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}