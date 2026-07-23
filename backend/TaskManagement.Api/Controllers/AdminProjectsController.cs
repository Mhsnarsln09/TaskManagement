using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Authentication;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Projects;

namespace TaskManagement.Api.Controllers;

// Admin management view (B10-08): lists every active project regardless of membership.
// The Admin role is the coarse gate here and is re-checked inside ProjectService.
[ApiController]
[Authorize(Roles = ApplicationRoles.Admin)]
[Route("api/admin/projects")]
[Produces("application/json")]
public sealed class AdminProjectsController(ProjectService projectService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List all active projects (admin)")]
    [ProducesResponseType<PagedResponse<ProjectResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<ProjectResponse>>> List(
        [FromQuery] PageQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
        {
            return ValidationProblem("Page must be positive and pageSize must be between 1 and 100.");
        }

        return Ok(await projectService.ListAllAsync(query, cancellationToken));
    }
}
