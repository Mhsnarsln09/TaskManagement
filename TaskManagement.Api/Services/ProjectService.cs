using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Contracts;
using TaskManagement.Api.Errors;
using TaskManagement.Domain.Projects;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Api.Services;

public sealed class ProjectService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    DevelopmentUserInitializer userInitializer)
{
    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        await userInitializer.EnsureCurrentUserExistsAsync(cancellationToken);

        var project = new Project(Guid.NewGuid(), request.Name, request.Description, currentUser.UserId);
        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(project);
    }

    public async Task<IReadOnlyCollection<ProjectResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.OwnerUserId == currentUser.UserId)
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

    public async Task<ProjectResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        ProjectResponse? project = await dbContext.Projects
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

        if (project is null)
        {
            throw new NotFoundException("Project was not found.");
        }

        EnsureProjectOwner(project.OwnerUserId);
        return project;
    }

    public async Task<ProjectResponse> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        Project project = await GetOwnedProjectEntityAsync(id, cancellationToken);

        project.Rename(request.Name);
        project.ChangeDescription(request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(project);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Project project = await GetOwnedProjectEntityAsync(id, cancellationToken);
        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Project> GetOwnedProjectEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        Project? project = await dbContext.Projects.SingleOrDefaultAsync(project => project.Id == id, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException("Project was not found.");
        }

        EnsureProjectOwner(project.OwnerUserId);
        return project;
    }

    private void EnsureProjectOwner(Guid ownerUserId)
    {
        if (ownerUserId != currentUser.UserId)
        {
            throw new ForbiddenException("You do not have access to this project.");
        }
    }

    private static ProjectResponse Map(Project project)
    {
        return new ProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.OwnerUserId,
            project.CreatedAtUtc,
            project.UpdatedAtUtc);
    }
}
