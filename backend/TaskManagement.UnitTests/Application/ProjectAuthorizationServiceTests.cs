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
    public async Task EnsureCanDeleteTasks_OwnerWithProjectManagerRole_Passes()
    {
        GrantMembership(_userId);
        SetOwner(_userId);
        GrantRole(ApplicationRoles.ProjectManager);

        await _service.EnsureCanDeleteTasksAsync(_projectId, CancellationToken.None);
    }

    [Fact]
    public async Task EnsureCanDeleteTasks_OwnerWithoutProjectManagerRole_ThrowsForbidden()
    {
        GrantMembership(_userId);
        SetOwner(_userId);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.EnsureCanDeleteTasksAsync(_projectId, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureCanDeleteTasks_MemberWithProjectManagerRoleButNotOwner_ThrowsForbidden()
    {
        GrantMembership(_userId);
        SetOwner(Guid.NewGuid());
        GrantRole(ApplicationRoles.ProjectManager);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _service.EnsureCanDeleteTasksAsync(_projectId, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureCanDeleteTasks_NonMember_ThrowsNotFound()
    {
        GrantRole(ApplicationRoles.ProjectManager);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.EnsureCanDeleteTasksAsync(_projectId, CancellationToken.None));
    }

    [Fact]
    public async Task EnsureCanDeleteTasks_AdminForExistingProject_Passes()
    {
        GrantRole(ApplicationRoles.Admin);
        _projectRepository.ExistsAsync(_projectId, Arg.Any<CancellationToken>()).Returns(true);

        await _service.EnsureCanDeleteTasksAsync(_projectId, CancellationToken.None);
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
