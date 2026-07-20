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

    // Managing a project (updating/deleting it, managing members) is restricted
    // to the project owner or an Admin.
    public async Task EnsureCanManageAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(ApplicationRoles.Admin))
        {
            await EnsureProjectExistsAsync(projectId, cancellationToken);
            return;
        }

        await EnsureOwnerAsync(projectId, cancellationToken);
    }

    // Task deletion is additionally role-gated (docs/tasks/05-business-rules.md):
    // the PRD assigns it to the project's manager, so the owner must also hold the
    // ProjectManager role. A Member-role owner keeps project management rights but
    // cannot delete tasks.
    public async Task EnsureCanDeleteTasksAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(ApplicationRoles.Admin))
        {
            await EnsureProjectExistsAsync(projectId, cancellationToken);
            return;
        }

        await EnsureOwnerAsync(projectId, cancellationToken);

        if (!currentUser.IsInRole(ApplicationRoles.ProjectManager))
        {
            throw new ForbiddenException("Only a project manager who owns the project or an admin can delete tasks.");
        }
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
