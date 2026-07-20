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

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await service.MarkAsReadAsync(id, cancellationToken);
        return NoContent();
    }
}
