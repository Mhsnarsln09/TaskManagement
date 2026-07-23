using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Authorization;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;
using TaskManagement.Domain.Tasks;
using TaskManagement.Application.Notifications;

namespace TaskManagement.Application.Tasks;

public sealed class TaskService(
    IProjectRepository projectRepository,
    ITaskRepository taskRepository,
    ProjectAuthorizationService projectAuthorization,
    TimeProvider timeProvider,
    INotificationService notificationService,
    IApplicationCache cache,
    ICurrentUser currentUser)
{
    public async Task<TaskResponse> CreateAsync(
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        // Task creation is a full-management action: project owner or Admin only (B10-02).
        await projectAuthorization.EnsureCanManageAsync(projectId, cancellationToken);
        await EnsureAssigneeIsMemberAsync(projectId, request.AssigneeUserId, cancellationToken);

        var task = new TaskItem(
            Guid.NewGuid(),
            projectId,
            request.Title,
            request.Description,
            request.Priority,
            request.DueDate,
            request.AssigneeUserId);

        await taskRepository.AddAsync(task, cancellationToken);
        await taskRepository.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(StatisticsCacheKey(projectId), cancellationToken);

        if (task.AssigneeUserId is Guid assigneeUserId)
        {
            await notificationService.TaskAssignedAsync(
                projectId, task.Id, assigneeUserId, task.Title, cancellationToken);
        }

        // The provider read back the row version on save, so it is now on the entity (B10-06).
        return Map(task, taskRepository.GetVersion(task));
    }

    public async Task<PagedResponse<TaskResponse>> ListAsync(
        Guid projectId,
        TaskListQuery query,
        CancellationToken cancellationToken)
    {
        await projectAuthorization.EnsureMemberAsync(projectId, cancellationToken);
        return await taskRepository.ListAsync(projectId, query, Today(), cancellationToken);
    }

    public async Task<TaskResponse> GetAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
    {
        await projectAuthorization.EnsureMemberAsync(projectId, cancellationToken);

        TaskResponse? task = await taskRepository.GetResponseAsync(projectId, taskId, Today(), cancellationToken);
        return task ?? throw new NotFoundException("Task was not found.");
    }

    public async Task<TaskResponse> UpdateAsync(
        Guid projectId,
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        // Membership is the 404 gate; the permission tier is decided below (B10-02).
        await projectAuthorization.EnsureMemberAsync(projectId, cancellationToken);

        TaskItem task = await GetTaskEntityAsync(projectId, taskId, cancellationToken);

        // Owner/Admin may edit every field and reassign. An ordinary member may only
        // change the status of a task assigned to them; any other field change (or a
        // task that is not theirs) is forbidden.
        bool canManage = await projectAuthorization.CanManageAsync(projectId, cancellationToken);
        if (!canManage)
        {
            if (task.AssigneeUserId != currentUser.UserId)
            {
                throw new ForbiddenException(
                    "Only the project owner, an admin, or the task's assignee can update this task.");
            }

            EnsureOnlyStatusChanged(task, request);
        }

        await EnsureAssigneeIsMemberAsync(projectId, request.AssigneeUserId, cancellationToken);

        // A past due date is rejected only when it is actually being changed (B10-05).
        // An overdue task keeps its stale date, so editing any other field must not be
        // blocked just because the existing due date is already in the past.
        if (request.DueDate is DateOnly newDueDate
            && newDueDate != task.DueDate
            && newDueDate < Today())
        {
            throw new ValidationProblemException(new Dictionary<string, string[]>
            {
                ["dueDate"] = ["Due date cannot be in the past."]
            });
        }

        Guid? previousAssignee = task.AssigneeUserId;
        WorkItemStatus previousStatus = task.Status;

        // Reopen policy: a completed task can only be changed after it is explicitly
        // reopened by requesting the InProgress status. Any other edit attempt on a
        // completed task is rejected by the domain with a conflict.
        if (task.Status == WorkItemStatus.Completed && request.Status == WorkItemStatus.InProgress)
        {
            task.Reopen();
        }

        task.Rename(request.Title);
        task.ChangeDescription(request.Description);
        task.ChangePriority(request.Priority);
        task.ChangeDueDate(request.DueDate);
        task.AssignTo(request.AssigneeUserId);
        ApplyStatus(task, request.Status);

        // Optimistic concurrency (B10-06): when the client sent the version it was
        // editing, pin it so a row changed by someone else fails with 409 instead of
        // overwriting the newer data.
        if (!string.IsNullOrWhiteSpace(request.Version))
        {
            taskRepository.SetExpectedVersion(task, request.Version);
        }

        await taskRepository.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(StatisticsCacheKey(projectId), cancellationToken);

        if (task.AssigneeUserId is Guid assigneeUserId && assigneeUserId != previousAssignee)
        {
            await notificationService.TaskAssignedAsync(projectId, task.Id, assigneeUserId, task.Title, cancellationToken);
        }

        if (task.AssigneeUserId is Guid statusRecipient && task.Status != previousStatus)
        {
            await notificationService.TaskStatusChangedAsync(
                projectId, task.Id, statusRecipient, task.Title, task.Status.ToString(), cancellationToken);
        }

        // The provider read back the row version on save, so it is now on the entity (B10-06).
        return Map(task, taskRepository.GetVersion(task));
    }

    public async Task DeleteAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
    {
        // Task deletion is a full-management action: project owner or Admin only (B10-02).
        await projectAuthorization.EnsureCanManageAsync(projectId, cancellationToken);

        TaskItem task = await GetTaskEntityAsync(projectId, taskId, cancellationToken);
        task.SoftDelete();
        await taskRepository.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(StatisticsCacheKey(projectId), cancellationToken);
    }

    // For the assignee status-only path: rejects any change to a non-status field so an
    // ordinary member cannot rename, reprioritise, reschedule or reassign a task. String
    // and description values are normalised the same way the domain setters would, so an
    // unchanged field never reads as a change because of whitespace.
    private static void EnsureOnlyStatusChanged(TaskItem task, UpdateTaskRequest request)
    {
        static string? Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        bool onlyStatus =
            Normalize(request.Title) == task.Title
            && Normalize(request.Description) == task.Description
            && request.Priority == task.Priority
            && request.DueDate == task.DueDate
            && request.AssigneeUserId == task.AssigneeUserId;

        if (!onlyStatus)
        {
            throw new ForbiddenException(
                "The task's assignee may only change its status; other fields are managed by the project owner or an admin.");
        }
    }

    private async Task EnsureAssigneeIsMemberAsync(
        Guid projectId,
        Guid? assigneeUserId,
        CancellationToken cancellationToken)
    {
        if (assigneeUserId is null)
        {
            return;
        }

        if (!await projectRepository.IsMemberAsync(projectId, assigneeUserId.Value, cancellationToken))
        {
            throw new ValidationProblemException(new Dictionary<string, string[]>
            {
                ["assigneeUserId"] = ["Assignee must be a member of the project."]
            });
        }
    }

    private async Task<TaskItem> GetTaskEntityAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskItem? task = await taskRepository.GetEntityAsync(projectId, taskId, cancellationToken);
        return task ?? throw new NotFoundException("Task was not found.");
    }

    // Allowed transitions (see docs/TECHNICAL-DECISIONS.md):
    // Todo -> InProgress | Cancelled, InProgress -> Completed | Cancelled,
    // Completed -> InProgress (reopen, handled in UpdateAsync). Cancelled is terminal.
    // Todo -> Completed is accepted as a convenience and runs Start + Complete.
    private static void ApplyStatus(TaskItem task, WorkItemStatus status)
    {
        if (task.Status == status)
        {
            return;
        }

        switch (status)
        {
            case WorkItemStatus.InProgress:
                task.Start();
                break;
            case WorkItemStatus.Completed:
                if (task.Status == WorkItemStatus.Todo)
                {
                    task.Start();
                }

                task.Complete();
                break;
            case WorkItemStatus.Cancelled:
                task.Cancel();
                break;
            case WorkItemStatus.Todo:
                throw new ConflictException("Task status cannot be moved back to todo.");
            default:
                throw new ConflictException("Task status change is not supported.");
        }
    }

    private DateOnly Today()
    {
        return DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
    }

    private static string StatisticsCacheKey(Guid projectId) => $"project-statistics:{projectId}";

    private TaskResponse Map(TaskItem task, string version)
    {
        return new TaskResponse(
            task.Id,
            task.ProjectId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.DueDate,
            task.AssigneeUserId,
            task.IsOverdue(Today()),
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            version);
    }
}
