using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Projects;

namespace TaskManagement.Application.Abstractions;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProjectResponse>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    // Admin management view: every active (non-soft-deleted) project, paged. Membership
    // is irrelevant here; the caller's Admin role is the gate (B10-08).
    Task<PagedResponse<ProjectResponse>> ListAllActiveAsync(
        PageQuery query,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> IsMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken);

    Task<Guid?> GetOwnerIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ProjectResponse?> GetResponseAsync(Guid id, CancellationToken cancellationToken);

    Task<Project?> GetEntityAsync(Guid id, CancellationToken cancellationToken);

    Task<Project?> GetEntityWithMembersAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProjectMemberResponse>> ListMembersAsync(
        Guid projectId,
        CancellationToken cancellationToken);

    void Remove(Project project);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
