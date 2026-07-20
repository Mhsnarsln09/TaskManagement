namespace TaskManagement.Application.Contracts;

public sealed record AdminUserListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null);

public sealed record ReplaceUserRolesRequest(IReadOnlyCollection<string> Roles);

public sealed record AdminUserResponse(
    Guid Id,
    string Email,
    string UserName,
    string? DisplayName,
    IReadOnlyCollection<string> Roles);
