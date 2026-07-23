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

        if (user is null)
        {
            return null;
        }

        // A locked-out account is rejected before the password is even checked, so a
        // guessing loop cannot keep probing once the lockout threshold is hit (B10-09).
        if (userManager.SupportsUserLockout && await userManager.IsLockedOutAsync(user))
        {
            return null;
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            // Record the failure; Identity locks the account after MaxFailedAccessAttempts.
            if (userManager.SupportsUserLockout)
            {
                await userManager.AccessFailedAsync(user);
            }

            return null;
        }

        // A successful login clears the failure counter so honest users are not locked
        // out by earlier typos.
        if (userManager.SupportsUserLockout)
        {
            await userManager.ResetAccessFailedCountAsync(user);
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

    public Task<string?> GetSecurityStampAsync(Guid userId, CancellationToken cancellationToken)
    {
        return userManager.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.SecurityStamp)
            .SingleOrDefaultAsync(cancellationToken);
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

    public async Task<IReadOnlyCollection<UserSummaryResponse>> SearchUsersAsync(
        string search,
        int limit,
        CancellationToken cancellationToken)
    {
        // NormalizedUserName is indexed and already upper-cased by Identity, so the
        // user-name half of the match stays sargable. DisplayName has no normalized
        // column, hence the explicit case-insensitive comparison.
        string normalized = userManager.NormalizeName(search);

        return await userManager.Users
            .AsNoTracking()
            .Where(user =>
                (user.NormalizedUserName != null && user.NormalizedUserName.Contains(normalized))
                || (user.DisplayName != null
                    && EF.Functions.ILike(user.DisplayName, $"%{search}%")))
            .OrderBy(user => user.DisplayName ?? user.UserName)
            .Take(limit)
            .Select(user => new UserSummaryResponse(
                user.Id,
                user.UserName ?? string.Empty,
                user.DisplayName))
            .ToListAsync(cancellationToken);
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
