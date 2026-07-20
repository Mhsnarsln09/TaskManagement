using TaskManagement.Application.Abstractions;

namespace TaskManagement.Api.Observability;

public sealed class CorrelationContext(IHttpContextAccessor accessor) : IAuditContext
{
    public Guid? UserId
    {
        get
        {
            string? value = accessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out Guid userId) ? userId : null;
        }
    }

    public string? CorrelationId => accessor.HttpContext?.TraceIdentifier;
}
