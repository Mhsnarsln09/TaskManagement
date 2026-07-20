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

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<AutoValidationActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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
builder.Services.AddAuthorization();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

await app.Services.SeedInfrastructureAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the entry point to WebApplicationFactory-based integration tests.
public partial class Program;
