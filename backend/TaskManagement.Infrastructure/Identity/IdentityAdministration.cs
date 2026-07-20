using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Authentication;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;
using TaskManagement.Infrastructure.Authentication;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Identity;

public sealed class IdentityAdministration(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ApplicationDbContext dbContext,
    IAuditContext auditContext,
    TimeProvider timeProvider) : IIdentityAdministration
{
    private const long RoleManagementLockId = 841_020_260_721;

    public async Task<PagedResponse<AdminUserResponse>> ListUsersAsync(
        AdminUserListQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<ApplicationUser> users = userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search.Trim()}%";
            users = users.Where(user =>
                EF.Functions.ILike(user.UserName!, pattern)
                || EF.Functions.ILike(user.Email!, pattern)
                || (user.DisplayName != null && EF.Functions.ILike(user.DisplayName, pattern)));
        }

        int totalCount = await users.CountAsync(cancellationToken);
        List<ApplicationUser> page = await users
            .OrderBy(user => user.UserName)
            .ThenBy(user => user.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var responses = new List<AdminUserResponse>(page.Count);
        foreach (ApplicationUser user in page)
        {
            responses.Add(await MapAsync(user));
        }

        return new PagedResponse<AdminUserResponse>(responses, query.Page, query.PageSize, totalCount);
    }

    public async Task<AdminUserResponse?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
        return user is null ? null : await MapAsync(user);
    }

    public async Task<int> CountUsersInRoleAsync(string role, CancellationToken cancellationToken)
    {
        IdentityRole<Guid>? identityRole = await roleManager.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.NormalizedName == role.ToUpperInvariant(), cancellationToken);
        return identityRole is null
            ? 0
            : await dbContext.UserRoles.CountAsync(item => item.RoleId == identityRole.Id, cancellationToken);
    }

    public async Task<AdminUserResponse> ReplaceRolesAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        // Role changes are rare. A transaction-level advisory lock closes the race
        // where two administrators could otherwise demote the final two SuperAdmins.
        await dbContext.Database.ExecuteSqlRawAsync(
            $"SELECT pg_advisory_xact_lock({RoleManagementLockId})",
            cancellationToken);

        ApplicationUser user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User was not found.");
        string[] currentRoles = (await userManager.GetRolesAsync(user))
            .Order(StringComparer.Ordinal)
            .ToArray();

        bool removesSuperAdmin = currentRoles.Contains(ApplicationRoles.SuperAdmin)
            && !roles.Contains(ApplicationRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase);
        if (removesSuperAdmin
            && await CountUsersInRoleAsync(ApplicationRoles.SuperAdmin, cancellationToken) <= 1)
        {
            throw new ConflictException("The last SuperAdmin role cannot be removed.");
        }

        string[] toRemove = currentRoles.Except(roles, StringComparer.OrdinalIgnoreCase).ToArray();
        string[] toAdd = roles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        EnsureSucceeded(await userManager.RemoveFromRolesAsync(user, toRemove));
        EnsureSucceeded(await userManager.AddToRolesAsync(user, toAdd));
        EnsureSucceeded(await userManager.UpdateSecurityStampAsync(user));

        DateTimeOffset now = timeProvider.GetUtcNow();
        List<RefreshToken> activeTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        activeTokens.ForEach(token => token.Revoke(now));

        string[] normalizedRoles = roles.Order(StringComparer.Ordinal).ToArray();
        dbContext.AuditLogs.Add(new AuditLog(
            "RolesChanged",
            nameof(ApplicationUser),
            userId.ToString(),
            auditContext.UserId,
            auditContext.CorrelationId,
            $"Old=[{string.Join(',', currentRoles)}]; New=[{string.Join(',', normalizedRoles)}]"));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AdminUserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            user.DisplayName,
            normalizedRoles);
    }

    private async Task<AdminUserResponse> MapAsync(ApplicationUser user)
    {
        IList<string> roles = await userManager.GetRolesAsync(user);
        return new AdminUserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.UserName ?? string.Empty,
            user.DisplayName,
            roles.Order(StringComparer.Ordinal).ToArray());
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new ValidationProblemException(result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray()));
    }
}
