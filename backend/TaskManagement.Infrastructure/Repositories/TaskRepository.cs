using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Tasks;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class TaskRepository(ApplicationDbContext dbContext) : ITaskRepository
{
    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        await dbContext.TaskItems.AddAsync(task, cancellationToken);
    }

    public async Task<PagedResponse<TaskResponse>> ListAsync(
        Guid projectId,
        TaskListQuery query,
        DateOnly today,
        CancellationToken cancellationToken)
    {
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
                task.DueDate != null
                    && task.DueDate < today
                    && task.Status != WorkItemStatus.Completed
                    && task.Status != WorkItemStatus.Cancelled,
                task.CreatedAtUtc,
                task.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResponse<TaskResponse>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<TaskResponse?> GetResponseAsync(
        Guid projectId,
        Guid taskId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        return await dbContext.TaskItems
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
                task.DueDate != null
                    && task.DueDate < today
                    && task.Status != WorkItemStatus.Completed
                    && task.Status != WorkItemStatus.Cancelled,
                task.CreatedAtUtc,
                task.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid projectId, Guid taskId, CancellationToken cancellationToken)
    {
        return dbContext.TaskItems
            .AsNoTracking()
            .AnyAsync(task => task.ProjectId == projectId && task.Id == taskId, cancellationToken);
    }

    // GroupBy over a constant collapses every count into one aggregate query instead
    // of six round trips. An empty project produces no group at all, which is why the
    // null result maps to zeroes rather than being treated as "project missing".
    public async Task<ProjectTaskCounts> GetCountsAsync(
        Guid projectId,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        ProjectTaskCounts? counts = await dbContext.TaskItems
            .AsNoTracking()
            .Where(task => task.ProjectId == projectId)
            .GroupBy(_ => 1)
            .Select(group => new ProjectTaskCounts(
                group.Count(),
                group.Count(task => task.Status == WorkItemStatus.Todo),
                group.Count(task => task.Status == WorkItemStatus.InProgress),
                group.Count(task => task.Status == WorkItemStatus.Completed),
                group.Count(task => task.Status == WorkItemStatus.Cancelled),
                group.Count(task => task.DueDate != null
                    && task.DueDate < today
                    && task.Status != WorkItemStatus.Completed
                    && task.Status != WorkItemStatus.Cancelled)))
            .SingleOrDefaultAsync(cancellationToken);

        return counts ?? new ProjectTaskCounts(0, 0, 0, 0, 0, 0);
    }

    public Task<bool> HasOpenTasksAssignedToUserAsync(
        Guid projectId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.TaskItems
            .AsNoTracking()
            .AnyAsync(
                task => task.ProjectId == projectId
                    && task.AssigneeUserId == userId
                    && task.Status != WorkItemStatus.Completed
                    && task.Status != WorkItemStatus.Cancelled,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<DueTaskReminderCandidate>> ListDueReminderCandidatesAsync(
        DateOnly dueDate,
        CancellationToken cancellationToken)
    {
        return await dbContext.TaskItems
            .AsNoTracking()
            .Where(task => task.DueDate == dueDate
                && task.AssigneeUserId != null
                && task.Status != WorkItemStatus.Completed
                && task.Status != WorkItemStatus.Cancelled)
            .Select(task => new DueTaskReminderCandidate(
                task.Id,
                task.AssigneeUserId!.Value,
                task.Title,
                task.DueDate!.Value))
            .ToListAsync(cancellationToken);
    }

    public async Task<TaskItem?> GetEntityAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        return await dbContext.TaskItems
            .SingleOrDefaultAsync(task => task.ProjectId == projectId && task.Id == taskId, cancellationToken);
    }

    public void Remove(TaskItem task)
    {
        dbContext.TaskItems.Remove(task);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
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
}
