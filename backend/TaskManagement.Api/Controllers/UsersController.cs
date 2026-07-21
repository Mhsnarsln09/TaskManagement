using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Users;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(UserDirectoryService service) : ControllerBase
{
    /// <summary>
    /// Looks up users by user name or display name so people can be picked by name
    /// instead of by GUID. Returns the safe public summary only: no e-mail, no roles.
    /// System role management stays on the SuperAdmin-only /api/admin/users route.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<UserSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyCollection<UserSummaryResponse>>> Search(
        [FromQuery] UserSearchQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await service.SearchAsync(query, cancellationToken));
    }
}
