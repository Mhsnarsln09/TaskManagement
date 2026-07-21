using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Users;

// Lets any signed-in user resolve a person by name so membership and assignment
// can be done from a picker instead of a pasted GUID.
//
// Exposure policy: the response is UserSummaryResponse (id, user name, optional
// display name). E-mail addresses and system roles stay out, so this endpoint
// cannot be used to harvest contact details or map out who holds privileges —
// that remains SuperAdmin-only through /api/admin/users.
public sealed class UserDirectoryService(IIdentityService identityService)
{
    // A search term is required and results are capped: this is a lookup, not a
    // "list every account" endpoint.
    public const int MinimumSearchLength = 2;
    public const int MaximumLimit = 25;

    public Task<IReadOnlyCollection<UserSummaryResponse>> SearchAsync(
        UserSearchQuery query,
        CancellationToken cancellationToken)
    {
        int limit = Math.Clamp(query.Limit, 1, MaximumLimit);
        return identityService.SearchUsersAsync(query.Search.Trim(), limit, cancellationToken);
    }
}
