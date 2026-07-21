namespace TaskManagement.Application.Contracts;

public sealed record RegisterRequest(
    string Email,
    string UserName,
    string Password,
    string? DisplayName);

public sealed record LoginRequest(string UserNameOrEmail, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    UserResponse User);

public sealed record UserResponse(
    Guid Id,
    string Email,
    string UserName,
    string? DisplayName,
    IReadOnlyCollection<string> Roles);

// Safe public projection of a user: no e-mail and no roles, so embedding an author
// or uploader in another resource cannot leak account details to project members.
public sealed record UserSummaryResponse(
    Guid Id,
    string UserName,
    string? DisplayName);

// Directory lookup so a project owner can pick a person by name instead of pasting
// a GUID. Deliberately requires a search term: it is a lookup, not an enumeration
// endpoint, and it returns the same safe projection as comment authors.
public sealed record UserSearchQuery(string Search, int Limit = 10);
