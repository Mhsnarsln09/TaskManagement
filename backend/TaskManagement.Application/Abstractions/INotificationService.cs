namespace TaskManagement.Application.Abstractions;

public interface INotificationService
{
    Task<bool> TaskAssignedAsync(Guid taskId, Guid userId, string title, CancellationToken cancellationToken);
    Task<bool> TaskStatusChangedAsync(Guid taskId, Guid userId, string title, string status, CancellationToken cancellationToken);
}
