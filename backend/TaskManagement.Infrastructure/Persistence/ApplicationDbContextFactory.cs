using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TaskManagement.Infrastructure.Persistence;

// Used only by "dotnet ef" design-time tooling. Keeping the tooling on this factory
// (startup project = Infrastructure) means Program.cs never runs, so migrations do not
// require the JWT signing key or the identity role seeding to succeed.
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    // Must match <UserSecretsId> in TaskManagement.Api.csproj.
    private const string ApiUserSecretsId = "TaskManagement.Api-Development-7d38dcbb";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddUserSecrets(ApiUserSecretsId)
            .AddEnvironmentVariables()
            .Build();

        // Generating migrations never opens a connection, so a placeholder is enough
        // when no configuration source provides the real connection string.
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=taskmanagement;Username=design-time;Password=design-time";

        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString)
                .Options;

        return new ApplicationDbContext(options);
    }
}
