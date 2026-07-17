using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.Api.Services;

public interface ICurrentUser
{
    Guid UserId { get; }
}

public sealed class DevelopmentCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            string? userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out Guid parsed)
                ? parsed
                : Guid.Parse("11111111-1111-1111-1111-111111111111");
        }
    }
}

public sealed class DevelopmentUserInitializer(
    UserManager<ApplicationUser> userManager,
    ICurrentUser currentUser)
{
    public async Task EnsureCurrentUserExistsAsync(CancellationToken cancellationToken)
    {
        if (await userManager.FindByIdAsync(currentUser.UserId.ToString()) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            Id = currentUser.UserId,
            UserName = "development-user",
            Email = "development@example.local",
            DisplayName = "Development User",
            EmailConfirmed = true
        };

        IdentityResult result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            string message = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Development user could not be created: {message}");
        }
    }
}
