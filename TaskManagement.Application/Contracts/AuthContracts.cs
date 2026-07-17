namespace TaskManagement.Application.Contracts;

public sealed record RegisterRequest(
    string Email,
    string UserName,
    string Password,
    string? DisplayName);

public sealed record LoginRequest(string UserNameOrEmail, string Password);

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    UserResponse User);

public sealed record UserResponse(
    Guid Id,
    string Email,
    string UserName,
    string? DisplayName,
    IReadOnlyCollection<string> Roles);
