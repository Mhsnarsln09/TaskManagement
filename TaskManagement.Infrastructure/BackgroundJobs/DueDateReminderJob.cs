using Hangfire;
using TaskManagement.Application.Notifications;

namespace TaskManagement.Infrastructure.BackgroundJobs;

[AutomaticRetry(Attempts = 3, DelaysInSeconds = [60, 300, 900], OnAttemptsExceeded = AttemptsExceededAction.Fail)]
[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class DueDateReminderJob(DueDateReminderService service)
{
    public Task ExecuteAsync() => service.RunAsync(CancellationToken.None);
}
