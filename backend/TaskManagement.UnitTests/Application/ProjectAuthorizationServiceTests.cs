using NSubstitute;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Authentication;
using TaskManagement.Application.Authorization;
using TaskManagement.Application.Errors;

namespace TaskManagement.UnitTests.Application;

// Covers the response policy documented on ProjectAuthorizationService:
// non-member -> 404 (no project id probing), member without permission -> 403,
// Admin bypasses membership but the project must still exist.
public sealed class ProjectAuthorizationServiceTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ProjectAuthorizationService _service;
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public ProjectAuthorizationServiceTests()
    {
        _currentUser.UserId.Returns(_userId);
        _service = new ProjectAuthorizationService(_projectRepository, _currentUser);
    }

    [Fact]
    public async Task EnsureMember_UserIsMember_Passes()
    {
        GrantMembership(_userId);

        await _service.EnsureMemberAsync(_projectId, CancellationToken.None);
    }

    [Fact]
    public async Task EnsureMember_UserIsNotMember_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.EnsureMemberAsync(_projectId, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureMember_AdminForExistingProject_SkipsMembershipCheck()
    {
        GrantRole(ApplicationRoles.Admin);
        _projectRepository.ExistsAsync(_projectId, Arg.Any<CancellationToken>()).Returns(true);

        await _service.EnsureMemberAsync(_projectId, CancellationToken.None);

        await _projectRepository.DidNotReceive()
            .IsMemberAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureMember_AdminForMissingProject_ThrowsNotFound()
    {
        GrantRole(ApplicationRoles.Admin);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.EnsureMemberAsync(_projectId, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureCanManage_UserIsOwner_Passes()
    {
        GrantMembership(_userId);
        SetOwner(_userId);

        await _service.EnsureCanManageAsync(_projectId, CancellationToken.None);
    }

    [Fact]
    public async Task EnsureCanManage_UserIsMemberButNotOwner_ThrowsForbidden()
    {
        GrantMembership(_userId);
        SetOwner(Guid.NewGuid());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.EnsureCanManageAsync(_projectId, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureCanManage_UserIsNotMember_ThrowsNotFound()
    {
        SetOwner(Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.EnsureCanManageAsync(_projectId, CancellationToken.None));
    }

    [Fact]
    public async Task CanManage_Owner_ReturnsTrue()
    {
        SetOwner(_userId);

        Assert.True(await _service.CanManageAsync(_projectId, CancellationToken.None));
    }

    [Fact]
    public async Task CanManage_Admin_ReturnsTrueWithoutOwnerLookup()
    {
        GrantRole(ApplicationRoles.Admin);

        Assert.True(await _service.CanManageAsync(_projectId, CancellationToken.None));
        await _projectRepository.DidNotReceive()
            .GetOwnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CanManage_MemberButNotOwner_ReturnsFalse()
    {
        // A system ProjectManager role alone must not grant project authority (B10-02).
        GrantRole(ApplicationRoles.ProjectManager);
        SetOwner(Guid.NewGuid());

        Assert.False(await _service.CanManageAsync(_projectId, CancellationToken.None));
    }

    private void GrantMembership(Guid userId)
    {
        _projectRepository.IsMemberAsync(_projectId, userId, Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private void SetOwner(Guid ownerId)
    {
        _projectRepository.GetOwnerIdAsync(_projectId, Arg.Any<CancellationToken>())
            .Returns(ownerId);
    }

    private void GrantRole(string role)
    {
        _currentUser.IsInRole(role).Returns(true);
    }
}
