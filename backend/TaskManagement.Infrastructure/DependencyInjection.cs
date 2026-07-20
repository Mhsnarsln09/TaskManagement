using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Files;
using TaskManagement.Infrastructure.Authentication;
using TaskManagement.Infrastructure.Files;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Repositories;
using TaskManagement.Infrastructure.Email;
using TaskManagement.Infrastructure.BackgroundJobs;
using Hangfire;
using Hangfire.PostgreSql;
using StackExchange.Redis;
using TaskManagement.Infrastructure.Caching;

namespace TaskManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<BootstrapAdminOptions>(
            configuration.GetSection(BootstrapAdminOptions.SectionName));
        // Password rules must stay in sync with RegisterRequestValidator so that a
        // request passing FluentValidation cannot be rejected later by Identity.
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.Configure<FileUploadOptions>(configuration.GetSection(FileUploadOptions.SectionName));
        services.Configure<LocalFileStorageOptions>(
            configuration.GetSection(LocalFileStorageOptions.SectionName));

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IIdentityAdministration, IdentityAdministration>();
        services.AddScoped<IAccessTokenGenerator, JwtTokenService>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<DueDateReminderJob>();

        string? redisConnection = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IApplicationCache, NoOpApplicationCache>();
        }
        else
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
            services.AddSingleton<IApplicationCache, RedisApplicationCache>();
        }

        if (configuration.GetValue("BackgroundJobs:Enabled", true))
        {
            services.AddHangfire(options => options
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(storage => storage.UseNpgsqlConnection(connectionString)));
            services.AddHangfireServer();
        }

        // Stateless and holding only a resolved root path, so a singleton avoids
        // re-creating the directory on every request.
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IFileScanner, BasicFileScanner>();

        return services;
    }

    public static Task SeedInfrastructureAsync(this IServiceProvider services)
    {
        return IdentityRoleSeeder.SeedAsync(services);
    }

    public static async Task MigrateInfrastructureAsync(this IServiceProvider services)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
