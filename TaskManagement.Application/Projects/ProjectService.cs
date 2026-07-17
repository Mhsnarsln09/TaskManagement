using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;
using TaskManagement.Domain.Projects;

namespace TaskManagement.Application.Projects;

public sealed class ProjectService(
    IProjectRepository projectRepository,
    ICurrentUser currentUser)
{
    public async Task<ProjectResponse> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = new Project(Guid.NewGuid(), request.Name, request.Description, currentUser.UserId);
        await projectRepository.AddAsync(project, cancellationToken);
        await projectRepository.SaveChangesAsync(cancellationToken);

        return Map(project);
    }

    public Task<IReadOnlyCollection<ProjectResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return projectRepository.ListByOwnerAsync(currentUser.UserId, cancellationToken);
    }

    public async Task<ProjectResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        ProjectResponse? project = await projectRepository.GetResponseAsync(id, cancellationToken);
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
        await projectRepository.SaveChangesAsync(cancellationToken);

        return Map(project);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        Project project = await GetOwnedProjectEntityAsync(id, cancellationToken);
        projectRepository.Remove(project);
        await projectRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Project> GetOwnedProjectEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        Project? project = await projectRepository.GetEntityAsync(id, cancellationToken);
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
