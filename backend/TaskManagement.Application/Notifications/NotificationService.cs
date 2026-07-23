using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;
using TaskManagement.Domain.Notifications;

namespace TaskManagement.Application.Notifications;

public sealed class NotificationService(
    INotificationRepository repository,
    IRealtimeNotifier realtimeNotifier,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : INotificationService
{
    public Task<PagedResponse<NotificationResponse>> ListAsync(PageQuery query, CancellationToken cancellationToken)
        => repository.ListAsync(currentUser.UserId, query, cancellationToken);

    public async Task<UnreadCountResponse> GetUnreadCountAsync(CancellationToken cancellationToken)
        => new(await repository.GetUnreadCountAsync(currentUser.UserId, cancellationToken));

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken)
    {
        Notification notification = await repository.GetAsync(id, currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Notification was not found.");
        notification.MarkAsRead(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
    }

    // Server-side "mark all read" in a single request; idempotent (B10-07).
    public Task MarkAllAsReadAsync(CancellationToken cancellationToken)
        => repository.MarkAllAsReadAsync(currentUser.UserId, timeProvider.GetUtcNow(), cancellationToken);

    // The message strings below are English fallbacks. The client localizes the primary
    // notification text from the structured Type (and navigates via ProjectId/TaskItemId);
    // fully parameterized event data is expanded with the collaboration events in B11-06.
    public Task<bool> TaskAssignedAsync(Guid projectId, Guid taskId, Guid userId, string title, CancellationToken cancellationToken)
        => CreateAsync(userId, projectId, taskId, NotificationType.TaskAssigned,
            $"Task assigned: {title}", $"assigned:{taskId}:{userId}", cancellationToken);

    public Task<bool> TaskStatusChangedAsync(Guid projectId, Guid taskId, Guid userId, string title, string status, CancellationToken cancellationToken)
        => CreateAsync(userId, projectId, taskId, NotificationType.TaskStatusChanged,
            $"Task '{title}' changed to {status}.", $"status:{taskId}:{status}:{timeProvider.GetUtcNow():O}", cancellationToken);

    public Task<bool> DueReminderAsync(Guid projectId, Guid taskId, Guid userId, string title, DateOnly dueDate, CancellationToken cancellationToken)
        => CreateAsync(userId, projectId, taskId, NotificationType.DueDateReminder,
            $"Task '{title}' is due on {dueDate:yyyy-MM-dd}.", $"due:{taskId}:{dueDate:yyyy-MM-dd}", cancellationToken);

    private async Task<bool> CreateAsync(
        Guid userId,
        Guid projectId,
        Guid taskId,
        NotificationType type,
        string message,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (await repository.ExistsAsync(idempotencyKey, cancellationToken)) return false;

        var notification = new Notification(Guid.NewGuid(), userId, projectId, taskId, type, message, idempotencyKey);
        await repository.AddAsync(notification, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        await realtimeNotifier.NotifyUserAsync(userId, Map(notification), cancellationToken);
        return true;
    }

    private static NotificationResponse Map(Notification notification) => new(
        notification.Id,
        notification.ProjectId,
        notification.TaskItemId,
        notification.Type,
        notification.Message,
        notification.ReadAtUtc is not null,
        notification.CreatedAtUtc,
        notification.ReadAtUtc);
}
