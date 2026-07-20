using Microsoft.Extensions.Logging;
using TaskManagement.Application.Abstractions;

namespace TaskManagement.Infrastructure.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string recipient, string subject, string body, string idempotencyKey, CancellationToken cancellationToken)
    {
        // Development adapter: deliberately avoids logging recipient/body because they
        // may contain personal data. Replace with a provider adapter in production.
        logger.LogInformation("Email accepted. Subject={Subject} IdempotencyKey={IdempotencyKey}", subject, idempotencyKey);
        return Task.CompletedTask;
    }
}
