using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Abstractions;

public interface IIdentityService
{
    Task<UserResponse> RegisterAsync(RegisterRequest request, string defaultRole);

    Task<UserResponse?> ValidateCredentialsAsync(string userNameOrEmail, string password);

    Task<bool> UserExistsAsync(Guid userId);
}
