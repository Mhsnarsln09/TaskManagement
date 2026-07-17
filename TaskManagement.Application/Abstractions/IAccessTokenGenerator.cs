using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Abstractions;

public interface IAccessTokenGenerator
{
    AuthResponse CreateToken(UserResponse user);
}
