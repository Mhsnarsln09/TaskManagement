using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;

namespace TaskManagement.Application.Notifications;

public sealed class DueDateReminderService(
    ITaskRepository taskRepository,
    IIdentityService identityService,
    IEmailSender emailSender,
    NotificationService notificationService,
    TimeProvider timeProvider)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        DateOnly tomorrow = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime).AddDays(1);
        IReadOnlyCollection<DueTaskReminderCandidate> candidates =
            await taskRepository.ListDueReminderCandidatesAsync(tomorrow, cancellationToken);

        foreach (DueTaskReminderCandidate candidate in candidates)
        {
            string idempotencyKey = $"due:{candidate.TaskItemId}:{candidate.DueDate:yyyy-MM-dd}";
            bool created = await notificationService.DueReminderAsync(
                candidate.ProjectId, candidate.TaskItemId, candidate.AssigneeUserId, candidate.Title, candidate.DueDate, cancellationToken);

            if (!created) continue;

            string? email = await identityService.GetEmailAsync(candidate.AssigneeUserId, cancellationToken);
            if (email is not null)
            {
                await emailSender.SendAsync(
                    email,
                    "Task due date reminder",
                    $"'{candidate.Title}' is due on {candidate.DueDate:yyyy-MM-dd}.",
                    idempotencyKey,
                    cancellationToken);
            }
        }
    }
}
