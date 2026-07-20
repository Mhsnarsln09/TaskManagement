namespace TaskManagement.Application.Abstractions;

public interface IAuditContext
{
    Guid? UserId { get; }
    string? CorrelationId { get; }
}
