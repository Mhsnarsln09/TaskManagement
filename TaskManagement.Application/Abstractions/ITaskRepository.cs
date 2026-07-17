using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Abstractions;

public interface ITaskRepository
{
    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    Task<PagedResponse<TaskResponse>> ListAsync(
        Guid projectId,
        TaskListQuery query,
        CancellationToken cancellationToken);

    Task<TaskResponse?> GetResponseAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken);

    Task<TaskItem?> GetEntityAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken);

    void Remove(TaskItem task);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
