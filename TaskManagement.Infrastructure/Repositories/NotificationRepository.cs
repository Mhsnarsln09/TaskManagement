using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Notifications;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class NotificationRepository(ApplicationDbContext dbContext) : INotificationRepository
{
    public Task AddAsync(Notification notification, CancellationToken cancellationToken)
        => dbContext.Notifications.AddAsync(notification, cancellationToken).AsTask();

    public Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken)
        => dbContext.Notifications.AsNoTracking().AnyAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

    public async Task<PagedResponse<NotificationResponse>> ListAsync(Guid userId, PageQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Notification> source = dbContext.Notifications.AsNoTracking().Where(x => x.UserId == userId);
        int total = await source.CountAsync(cancellationToken);
        List<NotificationResponse> items = await source
            .OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Select(x => new NotificationResponse(x.Id, x.TaskItemId, x.Type, x.Message,
                x.ReadAtUtc != null, x.CreatedAtUtc, x.ReadAtUtc))
            .ToListAsync(cancellationToken);
        return new PagedResponse<NotificationResponse>(items, query.Page, query.PageSize, total);
    }

    public Task<Notification?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken)
        => dbContext.Notifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
