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
    IApplicationCache cache)
{
    public async Task<TaskResponse> CreateAsync(
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        await projectAuthorization.EnsureMemberAsync(projectId, cancellationToken);
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
                task.Id, assigneeUserId, task.Title, cancellationToken);
        }

        return Map(task);
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
        await projectAuthorization.EnsureMemberAsync(projectId, cancellationToken);
        await EnsureAssigneeIsMemberAsync(projectId, request.AssigneeUserId, cancellationToken);

        TaskItem task = await GetTaskEntityAsync(projectId, taskId, cancellationToken);
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

        await taskRepository.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(StatisticsCacheKey(projectId), cancellationToken);

        if (task.AssigneeUserId is Guid assigneeUserId && assigneeUserId != previousAssignee)
        {
            await notificationService.TaskAssignedAsync(task.Id, assigneeUserId, task.Title, cancellationToken);
        }

        if (task.AssigneeUserId is Guid statusRecipient && task.Status != previousStatus)
        {
            await notificationService.TaskStatusChangedAsync(
                task.Id, statusRecipient, task.Title, task.Status.ToString(), cancellationToken);
        }

        return Map(task);
    }

    public async Task DeleteAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
    {
        // Only a ProjectManager owning the project or an Admin may delete tasks.
        await projectAuthorization.EnsureCanDeleteTasksAsync(projectId, cancellationToken);

        TaskItem task = await GetTaskEntityAsync(projectId, taskId, cancellationToken);
        task.SoftDelete();
        await taskRepository.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync(StatisticsCacheKey(projectId), cancellationToken);
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

    private TaskResponse Map(TaskItem task)
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
            task.UpdatedAtUtc);
    }
}
