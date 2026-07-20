using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Authentication;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;

namespace TaskManagement.Application.Administration;

public sealed class UserAdministrationService(IIdentityAdministration identityAdministration)
{
    public Task<PagedResponse<AdminUserResponse>> ListUsersAsync(
        AdminUserListQuery query,
        CancellationToken cancellationToken)
        => identityAdministration.ListUsersAsync(query, cancellationToken);

    public async Task<AdminUserResponse> ReplaceRolesAsync(
        Guid userId,
        ReplaceUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        AdminUserResponse user = await identityAdministration.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User was not found.");

        string[] roles = request.Roles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(role => ApplicationRoles.All.Single(
                known => known.Equals(role, StringComparison.OrdinalIgnoreCase)))
            .Order(StringComparer.Ordinal)
            .ToArray();

        bool removesSuperAdmin = user.Roles.Contains(
                ApplicationRoles.SuperAdmin,
                StringComparer.OrdinalIgnoreCase)
            && !roles.Contains(ApplicationRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase);

        if (removesSuperAdmin
            && await identityAdministration.CountUsersInRoleAsync(
                ApplicationRoles.SuperAdmin,
                cancellationToken) <= 1)
        {
            throw new ConflictException("The last SuperAdmin role cannot be removed.");
        }

        return await identityAdministration.ReplaceRolesAsync(userId, roles, cancellationToken);
    }
}
