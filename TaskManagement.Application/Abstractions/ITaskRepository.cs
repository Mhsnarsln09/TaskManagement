using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Abstractions;

public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    Task<PagedResponse<TaskResponse>> ListAsync(
        Guid projectId,
        TaskListQuery query,
        DateOnly today,
        CancellationToken cancellationToken);

    Task<TaskResponse?> GetResponseAsync(
        Guid projectId,
        Guid taskId,
        DateOnly today,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken);

    // Single projection over the project's tasks; see ProjectTaskCounts.
    Task<ProjectTaskCounts> GetCountsAsync(
        Guid projectId,
        DateOnly today,
        CancellationToken cancellationToken);

    Task<bool> HasOpenTasksAssignedToUserAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<TaskItem?> GetEntityAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken);

    void Remove(TaskItem task);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
