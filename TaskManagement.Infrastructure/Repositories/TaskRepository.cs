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
                task.CreatedAtUtc,
                task.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResponse<TaskResponse>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<TaskResponse?> GetResponseAsync(
        Guid projectId,
        Guid taskId,
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
                task.CreatedAtUtc,
                task.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
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
