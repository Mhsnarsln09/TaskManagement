using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Authorization;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;
using TaskManagement.Domain.Projects;

namespace TaskManagement.Application.Projects;

public sealed class ProjectService(
    IProjectRepository projectRepository,
    ITaskRepository taskRepository,
    IIdentityService identityService,
    ProjectAuthorizationService projectAuthorization,
    ICurrentUser currentUser)
{
    public async Task<ProjectResponse> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        // Project.Create seeds the owner as the first ProjectMember.
        Project project = Project.Create(Guid.NewGuid(), request.Name, request.Description, currentUser.UserId);
        await projectRepository.AddAsync(project, cancellationToken);
        await projectRepository.SaveChangesAsync(cancellationToken);

        return Map(project);
    }

    public Task<IReadOnlyCollection<ProjectResponse>> ListAsync(CancellationToken cancellationToken)
    {
        return projectRepository.ListForUserAsync(currentUser.UserId, cancellationToken);
    }

    public async Task<ProjectResponse> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await projectAuthorization.EnsureMemberAsync(id, cancellationToken);

        ProjectResponse? project = await projectRepository.GetResponseAsync(id, cancellationToken);
        return project ?? throw new NotFoundException("Project was not found.");
    }

    public async Task<ProjectResponse> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        await projectAuthorization.EnsureCanManageAsync(id, cancellationToken);
        Project project = await GetProjectEntityAsync(id, cancellationToken);

        project.Rename(request.Name);
        project.ChangeDescription(request.Description);
        await projectRepository.SaveChangesAsync(cancellationToken);

        return Map(project);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await projectAuthorization.EnsureCanManageAsync(id, cancellationToken);
        Project project = await GetProjectEntityAsync(id, cancellationToken);

        project.SoftDelete();
        await projectRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProjectMemberResponse>> ListMembersAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        await projectAuthorization.EnsureMemberAsync(id, cancellationToken);
        return await projectRepository.ListMembersAsync(id, cancellationToken);
    }

    public async Task<ProjectMemberResponse> AddMemberAsync(
        Guid id,
        AddProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        await projectAuthorization.EnsureCanManageAsync(id, cancellationToken);

        if (!await identityService.UserExistsAsync(request.UserId))
        {
            throw new ValidationProblemException(new Dictionary<string, string[]>
            {
                ["userId"] = ["User does not exist."]
            });
        }

        Project project = await GetProjectWithMembersAsync(id, cancellationToken);

        // Duplicate membership is rejected here as a business rule; the unique
        // (ProjectId, UserId) index is the database-level backstop.
        project.AddMember(request.UserId);
        await projectRepository.SaveChangesAsync(cancellationToken);

        ProjectMember member = project.Members.Single(member => member.UserId == request.UserId);
        return new ProjectMemberResponse(member.UserId, member.JoinedAtUtc);
    }

    public async Task RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        await projectAuthorization.EnsureCanManageAsync(id, cancellationToken);

        Project project = await GetProjectWithMembersAsync(id, cancellationToken);
        if (project.Members.All(member => member.UserId != userId))
        {
            throw new NotFoundException("Project member was not found.");
        }

        if (await taskRepository.HasOpenTasksAssignedToUserAsync(id, userId, cancellationToken))
        {
            throw new ConflictException(
                "User has open tasks assigned in this project. Reassign them before removing the member.");
        }

        project.RemoveMember(userId);
        await projectRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Project> GetProjectEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        Project? project = await projectRepository.GetEntityAsync(id, cancellationToken);
        return project ?? throw new NotFoundException("Project was not found.");
    }

    private async Task<Project> GetProjectWithMembersAsync(Guid id, CancellationToken cancellationToken)
    {
        Project? project = await projectRepository.GetEntityWithMembersAsync(id, cancellationToken);
        return project ?? throw new NotFoundException("Project was not found.");
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
