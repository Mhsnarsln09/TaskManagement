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
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAccessTokenGenerator, JwtTokenService>();

        // Stateless and holding only a resolved root path, so a singleton avoids
        // re-creating the directory on every request.
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        return services;
    }

    public static Task SeedInfrastructureAsync(this IServiceProvider services)
    {
        return IdentityRoleSeeder.SeedAsync(services);
    }
}
