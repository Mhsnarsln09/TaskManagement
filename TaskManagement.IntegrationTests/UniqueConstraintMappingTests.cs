using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TaskManagement.Api.Errors;
using TaskManagement.Domain.Projects;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.IntegrationTests;

// The duplicate-membership rule is normally caught in the domain, but a race
// between two requests can slip through to the unique (ProjectId, UserId) index.
// This verifies that the real DbUpdateException raised by the database backstop
// is mapped to 409 Conflict by the global exception handler instead of 500.
public sealed class UniqueConstraintMappingTests(TaskManagementApiFactory factory)
    : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task DatabaseUniqueViolation_IsMappedToConflict()
    {
        (Guid projectId, Guid ownerId) = await CreateProjectWithOwnerAsync();

        // Bypass the domain guard the way a racing request would and hit the
        // unique index directly.
        DbUpdateException exception;
        using (IServiceScope scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.ProjectMembers.Add(new ProjectMember(Guid.NewGuid(), projectId, ownerId));

            exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => dbContext.SaveChangesAsync());
        }

        var httpContext = new DefaultHttpContext();
        var problemDetailsService = new CapturingProblemDetailsService();
        var handler = new GlobalExceptionHandler(
            problemDetailsService,
            NullLogger<GlobalExceptionHandler>.Instance);

        bool handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
        Assert.Equal(StatusCodes.Status409Conflict, problemDetailsService.Problem?.Status);
    }

    private async Task<(Guid ProjectId, Guid OwnerId)> CreateProjectWithOwnerAsync()
    {
        HttpClient client = factory.CreateClient();
        string suffix = Guid.NewGuid().ToString("N")[..8];

        HttpResponseMessage register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"user{suffix}@test.local",
            userName = $"user{suffix}",
            password = "Password1",
            displayName = "Test User"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        using JsonDocument registerPayload = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        string token = registerPayload.RootElement.GetProperty("accessToken").GetString()!;
        Guid ownerId = registerPayload.RootElement.GetProperty("user").GetProperty("id").GetGuid();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage createProject = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "Race Project",
            description = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, createProject.StatusCode);
        using JsonDocument projectPayload = JsonDocument.Parse(await createProject.Content.ReadAsStringAsync());

        return (projectPayload.RootElement.GetProperty("id").GetGuid(), ownerId);
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? Problem { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            Problem = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            Problem = context.ProblemDetails;
            return ValueTask.FromResult(true);
        }
    }
}
