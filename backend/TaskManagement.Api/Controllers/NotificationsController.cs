using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Notifications;

namespace TaskManagement.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(NotificationService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<NotificationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<NotificationResponse>>> List(
        [FromQuery] PageQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
        {
            return ValidationProblem("Page must be positive and pageSize must be between 1 and 100.");
        }

        return Ok(await service.ListAsync(query, cancellationToken));
    }

    [HttpGet("unread-count")]
    [ProducesResponseType<UnreadCountResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadCountResponse>> UnreadCount(CancellationToken cancellationToken)
    {
        return Ok(await service.GetUnreadCountAsync(cancellationToken));
    }

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await service.MarkAsReadAsync(id, cancellationToken);
        return NoContent();
    }

    // Server-side "mark all read" in a single idempotent request (B10-07).
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await service.MarkAllAsReadAsync(cancellationToken);
        return NoContent();
    }
}
