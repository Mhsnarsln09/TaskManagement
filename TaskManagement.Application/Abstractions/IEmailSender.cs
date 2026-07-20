namespace TaskManagement.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string body, string idempotencyKey, CancellationToken cancellationToken);
}
