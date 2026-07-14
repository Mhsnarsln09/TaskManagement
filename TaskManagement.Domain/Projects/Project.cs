using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Projects;

public sealed class Project : Entity
{
    private readonly List<ProjectMember> _members = [];

    public Project(Guid id, string name, string? description, Guid ownerUserId)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name is required.", nameof(name));
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("Owner user id cannot be empty.", nameof(ownerUserId));
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        OwnerUserId = ownerUserId;

        _members.Add(new ProjectMember(Guid.NewGuid(), Id, ownerUserId));
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public Guid OwnerUserId { get; }

    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name is required.", nameof(name));
        }

        Name = name.Trim();
        MarkUpdated();
    }

    public void ChangeDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MarkUpdated();
    }

    public void AddMember(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id cannot be empty.", nameof(userId));
        }

        if (_members.Any(member => member.UserId == userId))
        {
            return;
        }

        _members.Add(new ProjectMember(Guid.NewGuid(), Id, userId));
        MarkUpdated();
    }
}
