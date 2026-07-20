using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Authorization;
using TaskManagement.Application.Errors;

namespace TaskManagement.Application.Tasks;

// Child resources of a task (comments, attachments) are addressed through the
// project route, so every one of them needs the same two checks: the caller must be a
// project member, and the task must actually belong to that project. Without the
// second check a member of project A could read comments of a task in project B by
// pairing their own project id with a foreign task id.
//
// Both failures answer 404 to match the response policy in docs/TECHNICAL-DECISIONS.md.
public sealed class TaskAccessGuard(
    ITaskRepository taskRepository,
    ProjectAuthorizationService projectAuthorization)
{
    public async Task EnsureTaskAccessibleAsync(
        Guid projectId,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        await projectAuthorization.EnsureMemberAsync(projectId, cancellationToken);

        if (!await taskRepository.ExistsAsync(projectId, taskId, cancellationToken))
        {
            throw new NotFoundException("Task was not found.");
        }
    }
}
