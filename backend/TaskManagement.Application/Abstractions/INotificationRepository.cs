using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Notifications;

namespace TaskManagement.Application.Abstractions;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task<PagedResponse<NotificationResponse>> ListAsync(Guid userId, PageQuery query, CancellationToken cancellationToken);
    Task<Notification?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken);

    // Total unread count for the user, independent of paging (B10-07).
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken);

    // Marks every unread notification of the user read in one server-side statement and
    // returns how many rows changed. Idempotent: a second call affects zero rows.
    Task<int> MarkAllAsReadAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
