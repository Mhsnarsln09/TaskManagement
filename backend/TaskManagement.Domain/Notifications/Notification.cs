using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Notifications;

public sealed class Notification : Entity
{
    public Notification(
        Guid id,
        Guid userId,
        Guid taskItemId,
        NotificationType type,
        string message,
        string idempotencyKey)
        : base(id)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User id cannot be empty.", nameof(userId));
        if (taskItemId == Guid.Empty) throw new ArgumentException("Task id cannot be empty.", nameof(taskItemId));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message is required.", nameof(message));
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));

        UserId = userId;
        TaskItemId = taskItemId;
        Type = type;
        Message = message.Trim();
        IdempotencyKey = idempotencyKey.Trim();
    }

    public Guid UserId { get; }
    public Guid TaskItemId { get; }
    public NotificationType Type { get; }
    public string Message { get; }
    public string IdempotencyKey { get; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    public void MarkAsRead(DateTimeOffset now)
    {
        if (ReadAtUtc is not null) return;
        ReadAtUtc = now;
        MarkUpdated();
    }
}
