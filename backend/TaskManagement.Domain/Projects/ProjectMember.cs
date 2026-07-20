using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Projects;

public sealed class ProjectMember : Entity
{
    public ProjectMember(Guid id, Guid projectId, Guid userId)
        : base(id)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project id cannot be empty.", nameof(projectId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        ProjectId = projectId;
        UserId = userId;
        JoinedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid ProjectId { get; }

    public Guid UserId { get; }

    public DateTimeOffset JoinedAtUtc { get; }
}
