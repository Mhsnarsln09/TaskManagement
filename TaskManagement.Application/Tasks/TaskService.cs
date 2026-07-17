using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;
using TaskManagement.Domain.Tasks;

namespace TaskManagement.Application.Tasks;

public sealed class TaskService(
    IProjectRepository projectRepository,
    ITaskRepository taskRepository,
    ICurrentUser currentUser)
{
    public async Task<TaskResponse> CreateAsync(
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureProjectAccessAsync(projectId, cancellationToken);
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

        return Map(task);
    }

    public async Task<PagedResponse<TaskResponse>> ListAsync(
        Guid projectId,
        TaskListQuery query,
        CancellationToken cancellationToken)
    {
        await EnsureProjectAccessAsync(projectId, cancellationToken);
        return await taskRepository.ListAsync(projectId, query, cancellationToken);
    }

    public async Task<TaskResponse> GetAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
    {
        await EnsureProjectAccessAsync(projectId, cancellationToken);

        TaskResponse? task = await taskRepository.GetResponseAsync(projectId, taskId, cancellationToken);
        return task ?? throw new NotFoundException("Task was not found.");
    }

    public async Task<TaskResponse> UpdateAsync(
        Guid projectId,
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureProjectAccessAsync(projectId, cancellationToken);
        await EnsureAssigneeIsMemberAsync(projectId, request.AssigneeUserId, cancellationToken);

        TaskItem task = await GetTaskEntityAsync(projectId, taskId, cancellationToken);

        task.Rename(request.Title);
        task.ChangeDescription(request.Description);
        task.ChangePriority(request.Priority);
        task.ChangeDueDate(request.DueDate);
        task.AssignTo(request.AssigneeUserId);
        ApplyStatus(task, request.Status);

        await taskRepository.SaveChangesAsync(cancellationToken);
        return Map(task);
    }

    public async Task DeleteAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
    {
        await EnsureProjectAccessAsync(projectId, cancellationToken);

        TaskItem task = await GetTaskEntityAsync(projectId, taskId, cancellationToken);
        taskRepository.Remove(task);
        await taskRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureProjectAccessAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await projectRepository.ExistsAsync(projectId, cancellationToken))
        {
            throw new NotFoundException("Project was not found.");
        }

        if (!await projectRepository.IsMemberAsync(projectId, currentUser.UserId, cancellationToken))
        {
            throw new ForbiddenException("You do not have access to this project.");
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

    private static TaskResponse Map(TaskItem task)
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
            task.CreatedAtUtc,
            task.UpdatedAtUtc);
    }
}
