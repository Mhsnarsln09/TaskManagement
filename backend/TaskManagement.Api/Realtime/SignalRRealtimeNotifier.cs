using Microsoft.AspNetCore.SignalR;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;

namespace TaskManagement.Api.Realtime;

public sealed class SignalRRealtimeNotifier(
    IHubContext<NotificationsHub> hub,
    ILogger<SignalRRealtimeNotifier> logger) : IRealtimeNotifier
{
    public async Task NotifyUserAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken)
    {
        try
        {
            await hub.Clients.User(userId.ToString())
                .SendAsync("notificationReceived", notification, cancellationToken);
        }
        catch (Exception exception)
        {
            // The durable notification is already stored. A transient websocket
            // failure must not turn the completed business operation into a 500.
            logger.LogWarning(exception, "Realtime notification delivery failed. NotificationId={NotificationId}", notification.Id);
        }
    }
}
