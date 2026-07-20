using NSubstitute;
using TaskManagement.Application.Abstractions;
using TaskManagement.Application.Administration;
using TaskManagement.Application.Authentication;
using TaskManagement.Application.Contracts;
using TaskManagement.Application.Errors;

namespace TaskManagement.UnitTests.Application;

public sealed class UserAdministrationServiceTests
{
    private readonly IIdentityAdministration _identityAdministration =
        Substitute.For<IIdentityAdministration>();

    [Fact]
    public async Task ReplaceRoles_RejectsRemovingLastSuperAdmin()
    {
        Guid userId = Guid.NewGuid();
        _identityAdministration.GetUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AdminUserResponse(
                userId,
                "admin@test.local",
                "admin",
                null,
                [ApplicationRoles.SuperAdmin]));
        _identityAdministration.CountUsersInRoleAsync(
                ApplicationRoles.SuperAdmin,
                Arg.Any<CancellationToken>())
            .Returns(1);
        var service = new UserAdministrationService(_identityAdministration);

        await Assert.ThrowsAsync<ConflictException>(() => service.ReplaceRolesAsync(
            userId,
            new ReplaceUserRolesRequest([ApplicationRoles.Admin]),
            CancellationToken.None));

        await _identityAdministration.DidNotReceive().ReplaceRolesAsync(
            Arg.Any<Guid>(),
            Arg.Any<IReadOnlyCollection<string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplaceRoles_NormalizesAndDeduplicatesKnownRoles()
    {
        Guid userId = Guid.NewGuid();
        _identityAdministration.GetUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new AdminUserResponse(userId, "user@test.local", "user", null, [ApplicationRoles.Member]));
        _identityAdministration.ReplaceRolesAsync(
                userId,
                Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => new AdminUserResponse(
                userId,
                "user@test.local",
                "user",
                null,
                call.ArgAt<IReadOnlyCollection<string>>(1)));
        var service = new UserAdministrationService(_identityAdministration);

        AdminUserResponse response = await service.ReplaceRolesAsync(
            userId,
            new ReplaceUserRolesRequest(["member", "ADMIN", "Member"]),
            CancellationToken.None);

        Assert.Equal([ApplicationRoles.Admin, ApplicationRoles.Member], response.Roles);
    }

    [Fact]
    public async Task ReplaceRoles_MissingUserThrowsNotFound()
    {
        Guid userId = Guid.NewGuid();
        _identityAdministration.GetUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns((AdminUserResponse?)null);
        var service = new UserAdministrationService(_identityAdministration);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ReplaceRolesAsync(
            userId,
            new ReplaceUserRolesRequest([ApplicationRoles.Member]),
            CancellationToken.None));
    }
}
