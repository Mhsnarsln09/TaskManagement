namespace TaskManagement.Application.Abstractions;

public interface INotificationService
{
    Task<bool> TaskAssignedAsync(Guid projectId, Guid taskId, Guid userId, string title, CancellationToken cancellationToken);
    Task<bool> TaskStatusChangedAsync(Guid projectId, Guid taskId, Guid userId, string title, string status, CancellationToken cancellationToken);
}
