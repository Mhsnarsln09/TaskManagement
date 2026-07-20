using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;

namespace TaskManagement.Application.Authentication;

public sealed class AuthService(
    IIdentityService identityService,
    IAccessTokenGenerator accessTokenGenerator)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        UserResponse user = await identityService.RegisterAsync(request, ApplicationRoles.Member);
        return await accessTokenGenerator.CreateSessionAsync(user, CancellationToken.None);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        UserResponse? user = await identityService.ValidateCredentialsAsync(
            request.UserNameOrEmail,
            request.Password);

        if (user is null)
        {
            throw new UnauthorizedException("Invalid username/email or password.");
        }

        return await accessTokenGenerator.CreateSessionAsync(user, CancellationToken.None);
    }

    public Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
        => accessTokenGenerator.RotateAsync(request.RefreshToken, cancellationToken);
}
