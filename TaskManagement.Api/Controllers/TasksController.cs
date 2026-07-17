using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.Contracts;
using TaskManagement.Api.Services;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/tasks")]
[Produces("application/json")]
public sealed class TasksController(TaskService taskService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Create(
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        TaskResponse task = await taskService.CreateAsync(projectId, request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { projectId, taskId = task.Id }, task);
    }

    [HttpGet]
    [ProducesResponseType<PagedResponse<TaskResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<TaskResponse>>> List(
        Guid projectId,
        [FromQuery] TaskListQuery query,
        CancellationToken cancellationToken)
    {
        PagedResponse<TaskResponse> tasks = await taskService.ListAsync(projectId, query, cancellationToken);
        return Ok(tasks);
    }

    [HttpGet("{taskId:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Get(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskResponse task = await taskService.GetAsync(projectId, taskId, cancellationToken);
        return Ok(task);
    }

    [HttpPut("{taskId:guid}")]
    [ProducesResponseType<TaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskResponse>> Update(
        Guid projectId,
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        TaskResponse task = await taskService.UpdateAsync(projectId, taskId, request, cancellationToken);
        return Ok(task);
    }

    [HttpDelete("{taskId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        await taskService.DeleteAsync(projectId, taskId, cancellationToken);
        return NoContent();
    }
}
