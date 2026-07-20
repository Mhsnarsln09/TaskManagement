using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Notifications;

namespace TaskManagement.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<PagedResponse<NotificationResponse>> ListAsync(Guid userId, PageQuery query, CancellationToken cancellationToken);
    Task<Notification?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
