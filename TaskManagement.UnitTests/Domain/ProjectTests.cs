using TaskManagement.Domain.Projects;

namespace TaskManagement.UnitTests.Domain;

public sealed class ProjectTests
{
    [Fact]
    public void Constructor_AddsOwnerAsProjectMember()
    {
        Guid ownerUserId = Guid.NewGuid();

        Project project = new(Guid.NewGuid(), "Project", null, ownerUserId);

        ProjectMember member = Assert.Single(project.Members);
        Assert.Equal(ownerUserId, member.UserId);
        Assert.Equal(project.Id, member.ProjectId);
    }

    [Fact]
    public void AddMember_IgnoresDuplicateUser()
    {
        Guid ownerUserId = Guid.NewGuid();
        Project project = new(Guid.NewGuid(), "Project", null, ownerUserId);

        project.AddMember(ownerUserId);

        Assert.Single(project.Members);
    }
}
