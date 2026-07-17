using System.Security.Claims;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Errors;

namespace TaskManagement.Api.Security;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            string? userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userId, out Guid parsed))
            {
                return parsed;
            }

            throw new UnauthorizedException("Authenticated user id claim is missing or invalid.");
        }
    }
}
