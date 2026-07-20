using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    public async Task<bool> UserExistsAsync(Guid userId)
    {
        return await userManager.FindByIdAsync(userId.ToString()) is not null;
    }

    public Task<string?> GetEmailAsync(Guid userId, CancellationToken cancellationToken)
    {
        return userManager.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.Email)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<UserResponse?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await userManager.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return user is null ? null : await MapAsync(user);
    }

    public async Task<IReadOnlyDictionary<Guid, UserSummaryResponse>> GetUserSummariesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return ReadOnlyDictionary<Guid, UserSummaryResponse>.Empty;
        }

        // Roles are not loaded here: the summary is deliberately limited to the
        // fields that are safe to show to any project member.
        return await userManager.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .Select(user => new UserSummaryResponse(
                user.Id,
                user.UserName ?? string.Empty,
                user.DisplayName))
            .ToDictionaryAsync(summary => summary.Id, cancellationToken);
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
