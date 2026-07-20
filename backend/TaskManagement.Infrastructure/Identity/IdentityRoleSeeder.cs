using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Authentication;
using Microsoft.Extensions.Options;

namespace TaskManagement.Infrastructure.Identity;

public static class IdentityRoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (string role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                IdentityResult result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                if (!result.Succeeded)
                {
                    string message = string.Join("; ", result.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Role '{role}' could not be seeded: {message}");
                }
            }
        }

        BootstrapAdminOptions options = scope.ServiceProvider
            .GetRequiredService<IOptions<BootstrapAdminOptions>>().Value;
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Email)
            || string.IsNullOrWhiteSpace(options.UserName)
            || string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException(
                "BootstrapAdmin is enabled but Email, UserName or Password is missing.");
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser? user = await userManager.FindByEmailAsync(options.Email)
            ?? await userManager.FindByNameAsync(options.UserName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = options.Email.Trim(),
                UserName = options.UserName.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(options.DisplayName)
                    ? null
                    : options.DisplayName.Trim()
            };
            EnsureSucceeded(await userManager.CreateAsync(user, options.Password));
        }
        else if (!string.Equals(user.Email, options.Email, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(user.UserName, options.UserName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "BootstrapAdmin Email and UserName resolve to an inconsistent existing account.");
        }

        if (!await userManager.IsInRoleAsync(user, ApplicationRoles.SuperAdmin))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, ApplicationRoles.SuperAdmin));
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        string message = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"SuperAdmin bootstrap failed: {message}");
    }
}
