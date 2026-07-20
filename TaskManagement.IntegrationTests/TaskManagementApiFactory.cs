using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.IntegrationTests;

// Boots the real API pipeline (validation filter, exception handler, JWT auth,
// identity seeding) against a real PostgreSQL instance started by Testcontainers,
// with the EF Core migrations applied — the same engine and schema as production.
//
// Isolation strategy: every test class receives its own factory via IClassFixture,
// so each test group runs against its own container and freshly migrated database.
// Tests inside a group create their own users/projects with unique names, so they
// never depend on shared or pre-existing data.
public sealed class TaskManagementApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Same major version as compose.yml so tests exercise the production engine.
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    // Attachment uploads must land in a throwaway directory instead of the repository
    // working tree, and each run gets its own so parallel runs cannot collide.
    public string StorageRootPath { get; } = Path.Combine(
        Path.GetTempPath(),
        $"taskmanagement-tests-{Guid.NewGuid():N}");

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();

        // The migrations must run before the host starts, because Program.cs seeds
        // identity roles during startup.
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();

        if (Directory.Exists(StorageRootPath))
        {
            Directory.Delete(StorageRootPath, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting is applied before Program.cs executes, so both the startup-time
        // JwtBearer configuration and the runtime IOptions<JwtOptions> binding see the
        // same values, and AddInfrastructure picks up the container's connection string.
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-1234567890");
        builder.UseSetting("FileStorage:Local:RootPath", StorageRootPath);
    }
}
