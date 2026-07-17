using TaskManagement.Application.Contracts;
using TaskManagement.Domain.Projects;

namespace TaskManagement.Application.Abstractions;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProjectResponse>> ListByOwnerAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken);

    Task<ProjectResponse?> GetResponseAsync(Guid id, CancellationToken cancellationToken);

    Task<Project?> GetEntityAsync(Guid id, CancellationToken cancellationToken);

    void Remove(Project project);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
