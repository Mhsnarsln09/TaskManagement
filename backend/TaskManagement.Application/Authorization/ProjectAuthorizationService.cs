using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Authentication;
using TaskManagement.Application.Errors;

namespace TaskManagement.Application.Authorization;

// Central resource-based authorization for project-scoped resources. Chosen over
// ASP.NET Core authorization handlers so the rules stay in the Application layer
// and can be unit tested without an HTTP pipeline.
//
// Response policy (documented in docs/TECHNICAL-DECISIONS.md):
// - Missing token -> 401 (JWT middleware).
// - Project missing OR caller is not a member -> 404, so non-members cannot
//   probe which project ids exist.
// - Caller is a member but lacks the required permission -> 403.
// - Admin bypasses membership checks (system-level role).
public sealed class ProjectAuthorizationService(
    IProjectRepository projectRepository,
    ICurrentUser currentUser)
{
    public async Task EnsureMemberAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(ApplicationRoles.Admin))
        {
            await EnsureProjectExistsAsync(projectId, cancellationToken);
            return;
        }

        if (!await projectRepository.IsMemberAsync(projectId, currentUser.UserId, cancellationToken))
        {
            throw new NotFoundException("Project was not found.");
        }
    }

    // Managing a project or its tasks in full (updating/deleting the project, managing
    // members, creating/editing/reassigning/deleting tasks) is restricted to the
    // project owner or an Admin. The PRD (docs/PRD.md §3-4) makes project ownership the
    // source of resource authority; the system ProjectManager role grants nothing on
    // its own (B10-02 removed the former ProjectManager+owner task-deletion exception).
    public async Task EnsureCanManageAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(ApplicationRoles.Admin))
        {
            await EnsureProjectExistsAsync(projectId, cancellationToken);
            return;
        }

        await EnsureOwnerAsync(projectId, cancellationToken);
    }

    // No-throw variant used by the task update matrix to distinguish a manager (owner
    // or Admin, full edit) from an ordinary member (assignee status-only). The calling
    // use-case has already established membership/existence, so this only decides the
    // permission tier and never leaks project existence.
    public async Task<bool> CanManageAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(ApplicationRoles.Admin))
        {
            return true;
        }

        Guid? ownerUserId = await projectRepository.GetOwnerIdAsync(projectId, cancellationToken);
        return ownerUserId == currentUser.UserId;
    }

    private async Task EnsureOwnerAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await EnsureMemberAsync(projectId, cancellationToken);

        Guid? ownerUserId = await projectRepository.GetOwnerIdAsync(projectId, cancellationToken);
        if (ownerUserId != currentUser.UserId)
        {
            throw new ForbiddenException("Only the project owner or an admin can perform this action.");
        }
    }

    private async Task EnsureProjectExistsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await projectRepository.ExistsAsync(projectId, cancellationToken))
        {
            throw new NotFoundException("Project was not found.");
        }
    }
}
