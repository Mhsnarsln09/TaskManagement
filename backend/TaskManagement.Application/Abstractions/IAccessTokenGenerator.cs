using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Abstractions;

public interface IAccessTokenGenerator
{
    Task<AuthResponse> CreateSessionAsync(UserResponse user, CancellationToken cancellationToken);
    Task<AuthResponse> RotateAsync(string refreshToken, CancellationToken cancellationToken);

    // Revokes the whole refresh-token family the given token belongs to (server-side
    // logout). Idempotent: an unknown, expired or already-revoked token is a no-op.
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);
}
