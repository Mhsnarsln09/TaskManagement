using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Abstractions;

public interface IAccessTokenGenerator
{
    Task<AuthResponse> CreateSessionAsync(UserResponse user, CancellationToken cancellationToken);
    Task<AuthResponse> RotateAsync(string refreshToken, CancellationToken cancellationToken);
}
