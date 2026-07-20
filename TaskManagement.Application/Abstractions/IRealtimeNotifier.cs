using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Abstractions;

public interface IRealtimeNotifier
{
    Task NotifyUserAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken);
}
