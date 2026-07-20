using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Abstractions;

public interface IIdentityService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request, string defaultRole);

    Task<UserResponse?> ValidateCredentialsAsync(string userNameOrEmail, string password);

    Task<bool> UserExistsAsync(Guid userId);

    // Resolves author/uploader ids to their safe public summary in one round trip.
    // Ids without a matching user are simply absent from the result.
    Task<IReadOnlyDictionary<Guid, UserSummaryResponse>> GetUserSummariesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);
}
