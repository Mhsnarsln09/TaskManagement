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
    Guid? AssigneeUserId);

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
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
