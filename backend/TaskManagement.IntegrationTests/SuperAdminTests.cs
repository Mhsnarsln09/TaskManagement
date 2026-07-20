using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace TaskManagement.IntegrationTests;

public sealed class SuperAdminTests(TaskManagementApiFactory factory)
    : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task Register_AlwaysCreatesMember_EvenWhenPayloadContainsRole()
    {
        HttpClient client = factory.CreateClient();
        string suffix = Guid.NewGuid().ToString("N")[..8];

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"member{suffix}@test.local",
            userName = $"member{suffix}",
            password = "Password1",
            displayName = "Member",
            role = "SuperAdmin"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string[] roles = payload.RootElement.GetProperty("user").GetProperty("roles")
            .EnumerateArray().Select(item => item.GetString()!).ToArray();
        Assert.Equal(["Member"], roles);
    }

    [Fact]
    public async Task AdminEndpoints_RejectMember_AndAllowSuperAdmin()
    {
        HttpClient member = factory.CreateClient();
        JsonElement memberSession = await RegisterAsync(member);
        member.DefaultRequestHeaders.Authorization = Bearer(memberSession.GetProperty("accessToken").GetString()!);

        Assert.Equal(HttpStatusCode.Forbidden, (await member.GetAsync("/api/admin/users")).StatusCode);

        HttpClient superAdmin = await CreateSuperAdminClientAsync();
        HttpResponseMessage response = await superAdmin.GetAsync("/api/admin/users?search=superadmin");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(1, payload.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task ReplaceRoles_RevokesExistingAccessAndRefreshTokens()
    {
        HttpClient targetClient = factory.CreateClient();
        JsonElement targetSession = await RegisterAsync(targetClient);
        Guid targetId = targetSession.GetProperty("user").GetProperty("id").GetGuid();
        string oldAccessToken = targetSession.GetProperty("accessToken").GetString()!;
        string oldRefreshToken = targetSession.GetProperty("refreshToken").GetString()!;
        HttpClient superAdmin = await CreateSuperAdminClientAsync();

        HttpResponseMessage changed = await superAdmin.PutAsJsonAsync(
            $"/api/admin/users/{targetId}/roles",
            new { roles = new[] { "ProjectManager", "Member" } });

        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            string? details = await dbContext.AuditLogs
                .Where(log => log.Action == "RolesChanged" && log.EntityId == targetId.ToString())
                .OrderByDescending(log => log.Id)
                .Select(log => log.Details)
                .FirstAsync();
            Assert.Equal("Old=[Member]; New=[Member,ProjectManager]", details);
        }

        targetClient.DefaultRequestHeaders.Authorization = Bearer(oldAccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await targetClient.GetAsync("/api/projects")).StatusCode);
        targetClient.DefaultRequestHeaders.Authorization = null;
        HttpResponseMessage refresh = await targetClient.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = oldRefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task ReplaceRoles_ValidatesUnknownRoleAndMissingUser()
    {
        HttpClient superAdmin = await CreateSuperAdminClientAsync();

        HttpResponseMessage unknownRole = await superAdmin.PutAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/roles",
            new { roles = new[] { "Owner" } });
        HttpResponseMessage missingUser = await superAdmin.PutAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/roles",
            new { roles = new[] { "Member" } });

        Assert.Equal(HttpStatusCode.BadRequest, unknownRole.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingUser.StatusCode);
    }

    [Fact]
    public async Task ReplaceRoles_ConcurrentUpdatesRemainConsistent()
    {
        HttpClient targetClient = factory.CreateClient();
        JsonElement targetSession = await RegisterAsync(targetClient);
        Guid targetId = targetSession.GetProperty("user").GetProperty("id").GetGuid();
        HttpClient firstAdmin = await CreateSuperAdminClientAsync();
        HttpClient secondAdmin = await CreateSuperAdminClientAsync();

        Task<HttpResponseMessage> first = firstAdmin.PutAsJsonAsync(
            $"/api/admin/users/{targetId}/roles",
            new { roles = new[] { "Member", "Admin" } });
        Task<HttpResponseMessage> second = secondAdmin.PutAsJsonAsync(
            $"/api/admin/users/{targetId}/roles",
            new { roles = new[] { "Member", "ProjectManager" } });
        HttpResponseMessage[] responses = await Task.WhenAll(first, second);

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        using IServiceScope scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser user = (await userManager.FindByIdAsync(targetId.ToString()))!;
        IList<string> roles = await userManager.GetRolesAsync(user);
        Assert.Contains("Member", roles);
        Assert.True(roles.Contains("Admin") ^ roles.Contains("ProjectManager"));
    }

    [Fact]
    public async Task ReplaceRoles_CannotDemoteLastSuperAdmin()
    {
        HttpClient superAdmin = await CreateSuperAdminClientAsync();
        using JsonDocument users = JsonDocument.Parse(
            await (await superAdmin.GetAsync("/api/admin/users?search=superadmin"))
                .Content.ReadAsStringAsync());
        Guid id = users.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        HttpResponseMessage response = await superAdmin.PutAsJsonAsync(
            $"/api/admin/users/{id}/roles",
            new { roles = new[] { "Admin" } });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Bootstrap_IsIdempotent()
    {
        await factory.Services.SeedInfrastructureAsync();
        await factory.Services.SeedInfrastructureAsync();

        using IServiceScope scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        List<ApplicationUser> superAdmins = (await userManager.GetUsersInRoleAsync("SuperAdmin")).ToList();
        Assert.Single(superAdmins);
        Assert.Equal("superadmin", superAdmins[0].UserName);
    }

    private async Task<HttpClient> CreateSuperAdminClientAsync()
    {
        HttpClient client = factory.CreateClient();
        HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userNameOrEmail = "superadmin",
            password = "SuperAdminPassword1"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = Bearer(
            payload.RootElement.GetProperty("accessToken").GetString()!);
        return client;
    }

    private static async Task<JsonElement> RegisterAsync(HttpClient client)
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"user{suffix}@test.local",
            userName = $"user{suffix}",
            password = "Password1",
            displayName = "Test User"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.Clone();
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);
}
