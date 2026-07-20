using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Abstractions;

public interface IIdentityAdministration
{
    Task<PagedResponse<AdminUserResponse>> ListUsersAsync(
        AdminUserListQuery query,
        CancellationToken cancellationToken);

    Task<AdminUserResponse?> GetUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<int> CountUsersInRoleAsync(string role, CancellationToken cancellationToken);

    Task<AdminUserResponse> ReplaceRolesAsync(
        Guid userId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);
}
