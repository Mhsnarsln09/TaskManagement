using Microsoft.AspNetCore.Identity;
using TaskManagement.Api.Contracts;
using TaskManagement.Api.Errors;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Api.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    JwtTokenService jwtTokenService)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
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

        await userManager.AddToRoleAsync(user, ApplicationRoles.Member);
        return await jwtTokenService.CreateTokenAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        ApplicationUser? user = await userManager.FindByNameAsync(request.UserNameOrEmail)
            ?? await userManager.FindByEmailAsync(request.UserNameOrEmail);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedException("Invalid username/email or password.");
        }

        return await jwtTokenService.CreateTokenAsync(user);
    }
}
