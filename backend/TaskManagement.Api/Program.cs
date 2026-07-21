using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using TaskManagement.Application;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Files;
using TaskManagement.Api.Errors;
using TaskManagement.Api.OpenApi;
using TaskManagement.Api.Security;
using TaskManagement.Api.Validation;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Authentication;
using TaskManagement.Api.Observability;
using TaskManagement.Api.Realtime;
using Serilog;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Hangfire;
using TaskManagement.Infrastructure.BackgroundJobs;
using TaskManagement.Application.Authentication;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TaskManagement.Api")
    .WriteTo.Console());

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<AutoValidationActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// The OpenAPI document generator reads Microsoft.AspNetCore.Http.Json options, not
// the MVC options above; without this the schema advertises integer enums while the
// API serializes strings.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Stops an oversized upload at the transport layer instead of buffering it only to
// have AttachmentService reject it. The headroom covers multipart boundary overhead
// so a file exactly on the limit still reaches the application-level check and gets a
// 400 with a readable message rather than a bare 413.
long maxUploadSizeInBytes = builder.Configuration
    .GetValue<long?>($"{FileUploadOptions.SectionName}:MaxSizeInBytes")
    ?? new FileUploadOptions().MaxSizeInBytes;
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxUploadSizeInBytes + 8 * 1024;
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHttpContextAccessor();
string[] allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});
builder.Services.AddScoped<IAuditContext, CorrelationContext>();
builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddSignalR();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddApplication();
JwtOptions jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey is not configured or is shorter than 32 bytes. "
        + "Set it via user secrets or environment variables before starting the API.");
}
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                string? userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                string? tokenStamp = context.Principal?.FindFirstValue("security_stamp");
                if (!Guid.TryParse(userIdClaim, out Guid userId) || string.IsNullOrWhiteSpace(tokenStamp))
                {
                    context.Fail("Token security stamp is missing or invalid.");
                    return;
                }

                IIdentityService identityService = context.HttpContext.RequestServices
                    .GetRequiredService<IIdentityService>();
                string? currentStamp = await identityService.GetSecurityStampAsync(
                    userId,
                    context.HttpContext.RequestAborted);
                if (!string.Equals(tokenStamp, currentStamp, StringComparison.Ordinal))
                {
                    context.Fail("Token is no longer valid.");
                }
            },
            OnMessageReceived = context =>
            {
                string? accessToken = context.Request.Query["access_token"];
                PathString path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken)
                    && path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "A valid bearer token is required.",
                    Instance = context.Request.Path
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "You do not have permission to access this resource.",
                    Instance = context.Request.Path
                });
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", policy =>
        policy.RequireRole(ApplicationRoles.SuperAdmin));
});
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
});
app.UseExceptionHandler();

if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    await app.Services.MigrateInfrastructureAsync();
}

await app.Services.SeedInfrastructureAsync();

if (app.Configuration.GetValue("BackgroundJobs:Enabled", true))
{
    IRecurringJobManager recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<DueDateReminderJob>(
        "due-date-reminders",
        job => job.ExecuteAsync(),
        Cron.Daily(7));
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationsHub>("/hubs/notifications");
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

// Exposes the entry point to WebApplicationFactory-based integration tests.
public partial class Program;
