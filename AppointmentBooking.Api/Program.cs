using AppointmentBooking.Api.Configuration;
using AppointmentBooking.Api.Exceptions;
using AppointmentBooking.Api.Models;
using AppointmentBooking.Api.OpenApi;
using AppointmentBooking.Api.RateLimiting;
using AppointmentBooking.Api.Repositories;
using AppointmentBooking.Api.Repositories.Auth;
using AppointmentBooking.Api.Repositories.BookingHolds;
using AppointmentBooking.Api.Repositories.Bookings;
using AppointmentBooking.Api.Repositories.Customers;
using AppointmentBooking.Api.Repositories.Employees;
using AppointmentBooking.Api.Repositories.EmployeeServices;
using AppointmentBooking.Api.Repositories.EmployeeWorkingHoursRepository;
using AppointmentBooking.Api.Repositories.EmployeeAvailabilityBlocks;
using AppointmentBooking.Api.Repositories.PasswordResetCodes;
using AppointmentBooking.Api.Repositories.Salons;
using AppointmentBooking.Api.Repositories.Services;
using AppointmentBooking.Api.Repositories.UserAccounts;
using AppointmentBooking.Api.Services.Auth;
using AppointmentBooking.Api.Services.BookedServices;
using AppointmentBooking.Api.Services.Bookings;
using AppointmentBooking.Api.Services.CurrentUser;
using AppointmentBooking.Api.Services.Email;
using AppointmentBooking.Api.Services.Employees;
using AppointmentBooking.Api.Services.EmployeeServices;
using AppointmentBooking.Api.Services.EmployeeWorkingHoursService;
using AppointmentBooking.Api.Services.EmployeeAvailabilityBlocks;
using AppointmentBooking.Api.Services.PasswordReset;
using AppointmentBooking.Api.Services.Profile;
using AppointmentBooking.Api.Services.Salons;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
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

// Railway terminates HTTPS at its edge and forwards plain HTTP to the API.
// X-Forwarded-Proto tells the API the original request was HTTPS. Railway overwrites this
// header at its edge and publishes no fixed proxy IP ranges, so no proxy address is pinned.
// Only the scheme is taken from forwarded headers; the client IP for rate limiting is read
// from X-Real-IP (RateLimiting:ClientIpHeader).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Liveness endpoint for the Railway deploy healthcheck (GET /health).
builder.Services.AddHealthChecks();

// Rate limiting (see RateLimiting/RateLimitingServiceCollectionExtensions.cs).
// On Railway set RateLimiting__ClientIpHeader=X-Real-IP.
builder.Services.AddApiRateLimiting(builder.Configuration);

// Login and reset-code limits per email address (RateLimiting/AttemptLimiter.cs).
builder.Services.AddSingleton<IAttemptLimiter, AttemptLimiter>();

// At most 4 password-hashing requests at a time for the whole API (RateLimiting/PasswordHashingLimiter.cs).
builder.Services.AddSingleton<PasswordHashingLimiter>();

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

// Configuration is validated when the API starts: a missing or invalid Railway variable
// stops the deploy with a clear message instead of failing on the first request.
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.SecretKey) &&
                   Encoding.UTF8.GetByteCount(options.SecretKey) >= 32,
        "Jwt:SecretKey must be set and at least 32 bytes long (Jwt__SecretKey).")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Issuer) &&
                   !string.IsNullOrWhiteSpace(options.Audience),
        "Jwt:Issuer and Jwt:Audience must be set.")
    .Validate(
        options => options.AccessTokenMinutes > 0 && options.RefreshTokenDays > 0,
        "Jwt:AccessTokenMinutes and Jwt:RefreshTokenDays must be greater than 0.")
    .ValidateOnStart();

builder.Services.AddOptions<RedisOptions>()
    .Bind(builder.Configuration.GetSection(RedisOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        "Redis:ConnectionString must be set (Redis__ConnectionString).")
    .ValidateOnStart();

OptionsBuilder<MailjetOptions> mailjetOptions = builder.Services.AddOptions<MailjetOptions>()
    .Bind(builder.Configuration.GetSection(MailjetOptions.SectionName));

// Local development may run without Mailjet; everywhere else the API does not start without it.
if (!builder.Environment.IsDevelopment())
{
    mailjetOptions
        .Validate(
            options => !string.IsNullOrWhiteSpace(options.ApiKey) &&
                       !string.IsNullOrWhiteSpace(options.SecretKey) &&
                       !string.IsNullOrWhiteSpace(options.FromEmail) &&
                       !string.IsNullOrWhiteSpace(options.FromName),
            "Mailjet:ApiKey, Mailjet:SecretKey, Mailjet:FromEmail and Mailjet:FromName must be set (Mailjet__*).")
        .ValidateOnStart();
}

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

builder.Services.AddOptions<BookingOptions>()
    .Bind(builder.Configuration.GetSection(BookingOptions.SectionName))
    .Validate(
        options => options.HoldDurationInMinutes > 0 && options.AvailabilitySlotIntervalMinutes > 0,
        "Bookings:HoldDurationInMinutes and Bookings:AvailabilitySlotIntervalMinutes must be greater than 0.")
    .Validate(
        options => options.MaxActiveBookingsPerCustomer > 0,
        "Bookings:MaxActiveBookingsPerCustomer must be greater than 0.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IEmailService, MailjetEmailService>(client =>
{
    client.BaseAddress = new Uri("https://api.mailjet.com/");
});

// Emails are sent in the background (see Services/Email/EmailBackgroundService.cs).
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddSingleton<IEmailQueue>(serviceProvider => serviceProvider.GetRequiredService<EmailQueue>());
builder.Services.AddHostedService<EmailBackgroundService>();
builder.Services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddScoped<IPasswordResetCodeRepository, PasswordResetCodeRepository>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IUserAccountRepository, UserAccountRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IBookableServiceRepository, BookableServiceRepository>();
builder.Services.AddScoped<IBookableServiceService, BookableServiceService>();
builder.Services.AddScoped<IEmployeeServiceRepository, EmployeeServiceRepository>();
builder.Services.AddScoped<IEmployeeServiceAssignmentService, EmployeeServiceAssignmentService>();
builder.Services.AddScoped<IEmployeeWorkingHoursRepository, EmployeeWorkingHoursRepository>();
builder.Services.AddScoped<IEmployeeWorkingHoursService, EmployeeWorkingHoursService>();
builder.Services.AddScoped<IEmployeeRecurringBreakService, EmployeeRecurringBreakService>();
builder.Services.AddScoped<IEmployeeUnavailabilityRepository, EmployeeUnavailabilityRepository>();
builder.Services.AddScoped<IEmployeeUnavailabilityService, EmployeeUnavailabilityService>();
builder.Services.AddScoped<IAccessTokenService, AccessTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<ISalonRepository, SalonRepository>();
builder.Services.AddScoped<ISalonService, SalonService>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();

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

// In production HTTPS is terminated by Railway at its edge; the container itself only
// listens on plain HTTP (port 8080), so a redirect here would only break the healthcheck.
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();

// After authentication so the booking-hold policy can count per signed-in user;
// before authorization and the controllers, so rejected requests do no further work.
app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health").DisableRateLimiting();

app.Run();

public partial class Program
{
}
