using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Statistics;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/statistics")]
[Produces("application/json")]
public sealed class ProjectStatisticsController(StatisticsService statisticsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProjectStatisticsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectStatisticsResponse>> Get(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        ProjectStatisticsResponse statistics = await statisticsService.GetAsync(projectId, cancellationToken);
        return Ok(statistics);
    }
}
