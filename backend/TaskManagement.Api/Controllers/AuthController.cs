using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaskManagement.Application.Authentication;
using TaskManagement.Application.Contracts;

namespace TaskManagement.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        AuthResponse response = await authService.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        AuthResponse response = await authService.LoginAsync(request);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await authService.RefreshAsync(request, cancellationToken));
    }

    // Idempotent server-side logout: revokes the refresh token's family. Always 204 so
    // repeated logouts and stale/expired tokens never surface an error to the client.
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Logout(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }
}
