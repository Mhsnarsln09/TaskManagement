using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Projects;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Repositories;

public sealed class ProjectRepository(ApplicationDbContext dbContext) : IProjectRepository
{
    public async Task AddAsync(Project project, CancellationToken cancellationToken)
    {
        await dbContext.Projects.AddAsync(project, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProjectResponse>> ListForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Members.Any(member => member.UserId == userId))
            .OrderBy(project => project.Name)
            .ThenBy(project => project.Id)
            .Select(project => new ProjectResponse(
                project.Id,
                project.Name,
                project.Description,
                project.OwnerUserId,
                project.CreatedAtUtc,
                project.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResponse<ProjectResponse>> ListAllActiveAsync(
        PageQuery query,
        CancellationToken cancellationToken)
    {
        // The Projects set carries the `!IsDeleted` filter, so soft-deleted projects are
        // excluded from the admin listing by default (B10-08).
        IQueryable<Project> projects = dbContext.Projects.AsNoTracking();

        int totalCount = await projects.CountAsync(cancellationToken);

        List<ProjectResponse> items = await projects
            .OrderBy(project => project.Name)
            .ThenBy(project => project.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(project => new ProjectResponse(
                project.Id,
                project.Name,
                project.Description,
                project.OwnerUserId,
                project.CreatedAtUtc,
                project.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ProjectResponse>(items, query.Page, query.PageSize, totalCount);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        // Uses the Projects set so the `!IsDeleted` query filter applies. This is the
        // Admin bypass path (ProjectAuthorizationService.EnsureProjectExistsAsync), so
        // a soft-deleted project also 404s for Admins, not just for members (B10-01).
        return dbContext.Projects
            .AsNoTracking()
            .AnyAsync(project => project.Id == id, cancellationToken);
    }

    public Task<bool> IsMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        // Membership is resolved through the Projects set (which carries the
        // `!IsDeleted` query filter) rather than querying ProjectMembers directly.
        // A soft-deleted project therefore never matches, so every membership-gated
        // path (task/comment/attachment/statistics access, assignee validation)
        // stops resolving its sub-resources once the project is deleted. See
        // docs/tasks/10-mvp-hardening.md B10-01.
        return dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Id == projectId)
            .AnyAsync(project => project.Members.Any(member => member.UserId == userId), cancellationToken);
    }

    public async Task<ProjectResponse?> GetResponseAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Id == id)
            .Select(project => new ProjectResponse(
                project.Id,
                project.Name,
                project.Description,
                project.OwnerUserId,
                project.CreatedAtUtc,
                project.UpdatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> GetOwnerIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Id == id)
            .Select(project => (Guid?)project.OwnerUserId)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Project?> GetEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Projects.SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
    }

    public async Task<Project?> GetEntityWithMembersAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Projects
            .Include(project => project.Members)
            .SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProjectMemberResponse>> ListMembersAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ProjectMembers
            .AsNoTracking()
            .Where(member => member.ProjectId == projectId)
            .OrderBy(member => member.JoinedAtUtc)
            .ThenBy(member => member.Id)
            .Select(member => new ProjectMemberResponse(member.UserId, null, member.JoinedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public void Remove(Project project)
    {
        dbContext.Projects.Remove(project);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
