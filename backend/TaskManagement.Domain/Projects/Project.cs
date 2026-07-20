using TaskManagement.Domain.Common;

namespace TaskManagement.Domain.Projects;

public sealed class Project : SoftDeletableEntity
{
    private readonly List<ProjectMember> _members = [];

    // EF Core materializes entities through this constructor; it must stay free
    // of side effects such as seeding the owner membership.
    private Project(Guid id, string name, string? description, Guid ownerUserId)
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
    }

    public static Project Create(Guid id, string name, string? description, Guid ownerUserId)
    {
        var project = new Project(id, name, description, ownerUserId);
        project._members.Add(new ProjectMember(Guid.NewGuid(), project.Id, ownerUserId));
        return project;
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
            throw new DomainException("User is already a member of this project.");
        }

        _members.Add(new ProjectMember(Guid.NewGuid(), Id, userId));
        MarkUpdated();
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == OwnerUserId)
        {
            throw new DomainException("The project owner cannot be removed from the project.");
        }

        ProjectMember? member = _members.SingleOrDefault(member => member.UserId == userId);
        if (member is null)
        {
            throw new DomainException("User is not a member of this project.");
        }

        _members.Remove(member);
        MarkUpdated();
    }
}
