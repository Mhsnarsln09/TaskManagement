using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Administration;
using TaskManagement.Application.Contracts;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "SuperAdminOnly")]
public sealed class AdminUsersController(UserAdministrationService userAdministration) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<AdminUserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<AdminUserResponse>>> List(
        [FromQuery] AdminUserListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await userAdministration.ListUsersAsync(query, cancellationToken));
    }

    [HttpPut("{userId:guid}/roles")]
    [ProducesResponseType<AdminUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserResponse>> ReplaceRoles(
        Guid userId,
        ReplaceUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await userAdministration.ReplaceRolesAsync(userId, request, cancellationToken));
    }
}
