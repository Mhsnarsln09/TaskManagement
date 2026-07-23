using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Comments;
using TaskManagement.Application.Contracts;

namespace TaskManagement.Api.Controllers;

// Nested under the project route like TasksController: the project id is what every
// authorization check is anchored on, so it stays visible in the URL.
[ApiController]
[Authorize]
[Route("api/projects/{projectId:guid}/tasks/{taskId:guid}/comments")]
[Produces("application/json")]
public sealed class TaskCommentsController(CommentService commentService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CommentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentResponse>> Create(
        Guid projectId,
        Guid taskId,
        CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        CommentResponse comment = await commentService.CreateAsync(projectId, taskId, request, cancellationToken);
        return CreatedAtAction(nameof(List), new { projectId, taskId }, comment);
    }

    [HttpGet]
    [EndpointSummary("List task comments, newest first")]
    [EndpointDescription(
        "Returns comments ordered newest first (descending CreatedAtUtc, then Id). " +
        "Page 1 carries the most recent comments; higher pages return progressively " +
        "older comments. The ordering is stable across pages even for comments sharing a timestamp.")]
    [ProducesResponseType<PagedResponse<CommentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResponse<CommentResponse>>> List(
        Guid projectId,
        Guid taskId,
        [FromQuery] CommentListQuery query,
        CancellationToken cancellationToken)
    {
        PagedResponse<CommentResponse> comments =
            await commentService.ListAsync(projectId, taskId, query, cancellationToken);
        return Ok(comments);
    }
}
