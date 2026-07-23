using TaskManagement.Domain.Notifications;

namespace TaskManagement.Application.Contracts;

public sealed record NotificationResponse(
    Guid Id,
    Guid ProjectId,
    Guid TaskItemId,
    NotificationType Type,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

// Total unread notifications for the current user; not bound to any page (B10-07).
public sealed record UnreadCountResponse(int UnreadCount);
