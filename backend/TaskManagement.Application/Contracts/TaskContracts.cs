using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Contracts;

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    DateOnly? DueDate,
    Guid? AssigneeUserId);

public sealed record UpdateTaskRequest(
    string Title,
    string? Description,
    WorkItemStatus Status,
    TaskPriority Priority,
    DateOnly? DueDate,
    Guid? AssigneeUserId,
    // Opaque optimistic-concurrency token echoed back from the TaskResponse the client
    // is editing (B10-06). When present it is enforced: a stale value is rejected with
    // 409 instead of silently overwriting a newer version. Managing clients (the web
    // app) always send it; it is optional only for backward compatibility.
    string? Version = null);

public sealed record TaskListQuery(
    int Page = 1,
    int PageSize = 20,
    WorkItemStatus? Status = null,
    TaskPriority? Priority = null,
    string? SortBy = null,
    string? SortDirection = null);

public sealed record TaskResponse(
    Guid Id,
    Guid ProjectId,
    string Title,
    string? Description,
    WorkItemStatus Status,
    TaskPriority Priority,
    DateOnly? DueDate,
    Guid? AssigneeUserId,
    bool IsOverdue,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    // Opaque optimistic-concurrency token (B10-06). The client stores it when it loads
    // a task and echoes it back in UpdateTaskRequest.Version so a stale edit is rejected.
    string Version);

public sealed record DueTaskReminderCandidate(
    Guid TaskItemId,
    Guid ProjectId,
    Guid AssigneeUserId,
    string Title,
    DateOnly DueDate);
