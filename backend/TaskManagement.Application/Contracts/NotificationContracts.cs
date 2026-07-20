using TaskManagement.Domain.Notifications;

namespace TaskManagement.Application.Contracts;

public sealed record NotificationResponse(
    Guid Id,
    Guid TaskItemId,
    NotificationType Type,
    string Message,
    bool IsRead,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);
