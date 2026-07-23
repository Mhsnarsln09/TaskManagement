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

    Task<IReadOnlyCollection<DueTaskReminderCandidate>> ListDueReminderCandidatesAsync(
        DateOnly dueDate,
        CancellationToken cancellationToken);

    Task<TaskItem?> GetEntityAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken);

    // Optimistic concurrency (B10-06): pins the expected row version so the UPDATE only
    // succeeds when the row still matches the version the client last read. A stale
    // version yields a DbUpdateConcurrencyException, mapped to 409. An unparseable token
    // is treated as a conflict rather than a server error.
    void SetExpectedVersion(TaskItem task, string version);

    // Current optimistic-concurrency token of a tracked task. After SaveChanges the
    // provider has read back the new row version, so this reflects the persisted value.
    string GetVersion(TaskItem task);

    void Remove(TaskItem task);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
