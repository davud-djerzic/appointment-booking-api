using AppointmentBooking.Api.BackgroundServices;
using AppointmentBooking.Api.Configuration;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.OpenApi;
using AppointmentBooking.Api.Repositories;
using AppointmentBooking.Api.Repositories.Appointments;
using AppointmentBooking.Api.Repositories.Auth;
using AppointmentBooking.Api.Repositories.BookingHolds;
using AppointmentBooking.Api.Repositories.Bookings;
using AppointmentBooking.Api.Repositories.Customers;
using AppointmentBooking.Api.Repositories.Employees;
using AppointmentBooking.Api.Repositories.EmployeeServices;
using AppointmentBooking.Api.Repositories.EmployeeWorkingHoursRepository;
using AppointmentBooking.Api.Repositories.Services;
using AppointmentBooking.Api.Repositories.UserAccounts;
using AppointmentBooking.Api.Services.Appointments;
using AppointmentBooking.Api.Services.Auth;
using AppointmentBooking.Api.Services.BookedServices;
using AppointmentBooking.Api.Services.Bookings;
using AppointmentBooking.Api.Services.CurrentUser;
using AppointmentBooking.Api.Services.Employees;
using AppointmentBooking.Api.Services.EmployeeServices;
using AppointmentBooking.Api.Services.EmployeeWorkingHoursService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
         .WithOrigins(
             "http://localhost:5173",
             "https://localhost:5173")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        JwtOptions jwtOptions =
            builder.Configuration
                .GetSection(JwtOptions.SectionName)
                .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "JWT configuration is missing.");

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.SecretKey)),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero,

                NameClaimType = JwtRegisteredClaimNames.Sub,
                RoleClaimType = ClaimTypes.Role
            };
    });

builder.Services.AddAuthorization();

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

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));

builder.Services.AddSingleton<NpgsqlDataSource>(_ =>
    NpgsqlDataSource.Create(connectionString));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    RedisOptions redisOptions =
        builder.Configuration
            .GetSection(RedisOptions.SectionName)
            .Get<RedisOptions>()
        ?? throw new InvalidOperationException(
            "Redis configuration is missing.");

    return ConnectionMultiplexer.Connect(
        redisOptions.ConnectionString);
});

builder.Services.AddSingleton<IBookingHoldRepository, BookingHoldRepository>();

builder.Services.Configure<BookingOptions>(builder.Configuration.GetSection(BookingOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<ExpiredAppointmnetCleanupService>();
builder.Services.AddHostedService<AutomaticBookingCompletionService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddScoped<IUserAccountRepository, UserAccountRepository>();
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
builder.Services.AddScoped<IAccessTokenService, AccessTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}