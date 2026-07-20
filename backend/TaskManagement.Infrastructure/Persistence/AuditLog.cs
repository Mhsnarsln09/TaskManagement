namespace TaskManagement.Infrastructure.Persistence;

public sealed class AuditLog
{
    public long Id { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public Guid? UserId { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public AuditLog(string action, string entityType, string entityId, Guid? userId, string? correlationId)
    {
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        UserId = userId;
        CorrelationId = correlationId;
        OccurredAtUtc = DateTimeOffset.UtcNow;
    }

    private AuditLog() { }
}
