using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Contracts;
using TaskManagement.Api.Errors;
using TaskManagement.Domain.Tasks;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Api.Services;

public sealed class TaskService(ApplicationDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<TaskResponse> CreateAsync(
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureProjectAccessAsync(projectId, cancellationToken);

        var task = new TaskItem(
            Guid.NewGuid(),
            projectId,
            request.Title,
            request.Description,
            request.Priority,
            request.DueDate,
            request.AssigneeUserId);

        dbContext.TaskItems.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(task);
    }

    public async Task<PagedResponse<TaskResponse>> ListAsync(
        Guid projectId,
        TaskListQuery query,
        CancellationToken cancellationToken)
    {
        await EnsureProjectAccessAsync(projectId, cancellationToken);

        IQueryable<TaskItem> tasks = dbContext.TaskItems
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId);

        if (query.Status is not null)
        {
            tasks = tasks.Where(task => task.Status == query.Status);
        }

        if (query.Priority is not null)
        {
            tasks = tasks.Where(task => task.Priority == query.Priority);
        }

        int totalCount = await tasks.CountAsync(cancellationToken);

        tasks = ApplySorting(tasks, query.SortBy, query.SortDirection);

        List<TaskResponse> items = await tasks
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(task => new TaskResponse(
                task.Id,
                task.ProjectId,
                task.Title,
                task.Description,
                task.Status,
                task.Priority,
                task.DueDate,
                task.AssigneeUserId,
                task.CreatedAtUtc,
                task.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResponse<TaskResponse>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<TaskResponse> GetAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
    {
        await EnsureProjectAccessAsync(projectId, cancellationToken);

        TaskResponse? task = await dbContext.TaskItems
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId && task.Id == taskId)
            .Select(task => new TaskResponse(
                task.Id,
                task.ProjectId,
                task.Title,
                task.Description,
                task.Status,
                task.Priority,
                task.DueDate,
                task.AssigneeUserId,
                task.CreatedAtUtc,
                task.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        return task ?? throw new NotFoundException("Task was not found.");
    }

    public async Task<TaskResponse> UpdateAsync(
        Guid projectId,
        Guid taskId,
        UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureProjectAccessAsync(projectId, cancellationToken);

        TaskItem task = await GetTaskEntityAsync(projectId, taskId, cancellationToken);

        task.Rename(request.Title);
        task.ChangeDescription(request.Description);
        task.ChangePriority(request.Priority);
        task.ChangeDueDate(request.DueDate);
        task.AssignTo(request.AssigneeUserId);
        ApplyStatus(task, request.Status);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(task);
    }

    public async Task DeleteAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
    {
        await EnsureProjectAccessAsync(projectId, cancellationToken);

        TaskItem task = await GetTaskEntityAsync(projectId, taskId, cancellationToken);
        dbContext.TaskItems.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureProjectAccessAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .Select(project => new { project.OwnerUserId })
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            throw new NotFoundException("Project was not found.");
        }

        if (project.OwnerUserId != currentUser.UserId)
        {
            throw new ForbiddenException("You do not have access to this project.");
        }
    }

    private async Task<TaskItem> GetTaskEntityAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        TaskItem? task = await dbContext.TaskItems
            .SingleOrDefaultAsync(task => task.ProjectId == projectId && task.Id == taskId, cancellationToken);

        return task ?? throw new NotFoundException("Task was not found.");
    }

    private static IQueryable<TaskItem> ApplySorting(
        IQueryable<TaskItem> tasks,
        string? sortBy,
        string? sortDirection)
    {
        bool descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.ToLowerInvariant()) switch
        {
            "title" => descending
                ? tasks.OrderByDescending(task => task.Title).ThenBy(task => task.Id)
                : tasks.OrderBy(task => task.Title).ThenBy(task => task.Id),
            "status" => descending
                ? tasks.OrderByDescending(task => task.Status).ThenBy(task => task.Id)
                : tasks.OrderBy(task => task.Status).ThenBy(task => task.Id),
            "priority" => descending
                ? tasks.OrderByDescending(task => task.Priority).ThenBy(task => task.Id)
                : tasks.OrderBy(task => task.Priority).ThenBy(task => task.Id),
            "duedate" => descending
                ? tasks.OrderByDescending(task => task.DueDate).ThenBy(task => task.Id)
                : tasks.OrderBy(task => task.DueDate).ThenBy(task => task.Id),
            "createdatutc" or null or "" => descending
                ? tasks.OrderByDescending(task => task.CreatedAtUtc).ThenBy(task => task.Id)
                : tasks.OrderBy(task => task.CreatedAtUtc).ThenBy(task => task.Id),
            _ => tasks.OrderBy(task => task.CreatedAtUtc).ThenBy(task => task.Id)
        };
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
