using Microsoft.AspNetCore.Identity;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;

namespace TaskManagement.Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<UserResponse> RegisterAsync(RegisterRequest request, string defaultRole)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim()
        };

        IdentityResult result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ValidationProblemException(result.Errors
                .GroupBy(error => error.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Description).ToArray()));
        }

        await userManager.AddToRoleAsync(user, defaultRole);
        return await MapAsync(user);
    }

    public async Task<UserResponse?> ValidateCredentialsAsync(string userNameOrEmail, string password)
    {
        ApplicationUser? user = await userManager.FindByNameAsync(userNameOrEmail)
            ?? await userManager.FindByEmailAsync(userNameOrEmail);

        if (user is null || !await userManager.CheckPasswordAsync(user, password))
        {
            return null;
        }

        return await MapAsync(user);
    }

    private async Task<UserResponse> MapAsync(ApplicationUser user)
    {
        IList<string> roles = await userManager.GetRolesAsync(user);

        return new UserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            user.DisplayName,
            roles.ToArray());
    }
}
