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

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Projects
            .AsNoTracking()
            .AnyAsync(project => project.Id == id, cancellationToken);
    }

    public Task<bool> IsMemberAsync(Guid projectId, Guid userId, CancellationToken cancellationToken)
    {
        return dbContext.ProjectMembers
            .AsNoTracking()
            .AnyAsync(
                member => member.ProjectId == projectId && member.UserId == userId,
                cancellationToken);
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

    public async Task<Project?> GetEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Projects.SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
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
