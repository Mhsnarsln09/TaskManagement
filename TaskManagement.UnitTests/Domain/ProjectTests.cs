using TaskManagement.Domain.Common;
using TaskManagement.Domain.Projects;

namespace TaskManagement.UnitTests.Domain;

public sealed class ProjectTests
{
    [Fact]
    public void Constructor_AddsOwnerAsProjectMember()
    {
        Guid ownerUserId = Guid.NewGuid();

        Project project = Project.Create(Guid.NewGuid(), "Project", null, ownerUserId);

        ProjectMember member = Assert.Single(project.Members);
        Assert.Equal(ownerUserId, member.UserId);
        Assert.Equal(project.Id, member.ProjectId);
    }

    [Fact]
    public void AddMember_RejectsDuplicateUser()
    {
        Guid ownerUserId = Guid.NewGuid();
        Project project = Project.Create(Guid.NewGuid(), "Project", null, ownerUserId);

        Assert.Throws<DomainException>(() => project.AddMember(ownerUserId));
        Assert.Single(project.Members);
    }

    [Fact]
    public void AddMember_AddsNewUser()
    {
        Project project = Project.Create(Guid.NewGuid(), "Project", null, Guid.NewGuid());
        Guid userId = Guid.NewGuid();

        project.AddMember(userId);

        Assert.Equal(2, project.Members.Count);
        Assert.Contains(project.Members, member => member.UserId == userId);
    }

    [Fact]
    public void RemoveMember_RemovesExistingMember()
    {
        Project project = Project.Create(Guid.NewGuid(), "Project", null, Guid.NewGuid());
        Guid userId = Guid.NewGuid();
        project.AddMember(userId);

        project.RemoveMember(userId);

        Assert.Single(project.Members);
        Assert.DoesNotContain(project.Members, member => member.UserId == userId);
    }

    [Fact]
    public void RemoveMember_RejectsOwner()
    {
        Guid ownerUserId = Guid.NewGuid();
        Project project = Project.Create(Guid.NewGuid(), "Project", null, ownerUserId);

        Assert.Throws<DomainException>(() => project.RemoveMember(ownerUserId));
    }

    [Fact]
    public void RemoveMember_RejectsUnknownUser()
    {
        Project project = Project.Create(Guid.NewGuid(), "Project", null, Guid.NewGuid());

        Assert.Throws<DomainException>(() => project.RemoveMember(Guid.NewGuid()));
    }
}
