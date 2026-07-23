using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagement.IntegrationTests;

// Covers docs/tasks/10-mvp-hardening.md B10-08: an Admin can list every active project
// through the management endpoint, ordinary users still only see their own memberships,
// and soft-deleted projects are excluded from the default admin listing.
public sealed class AdminProjectViewTests(TaskManagementApiFactory factory)
    : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task Admin_ListsAllActiveProjects_IncludingOnesTheyDoNotBelongTo()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner, "Admin-visible project");
        HttpClient admin = await CreateAdminClientAsync();

        HttpResponseMessage response = await admin.GetAsync("/api/admin/projects?page=1&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Guid[] ids = payload.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToArray();
        Assert.Contains(projectId, ids);
    }

    [Fact]
    public async Task Admin_DoesNotSeeSoftDeletedProjects_InTheDefaultListing()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid keep = await CreateProjectAsync(owner, "Kept project");
        Guid removed = await CreateProjectAsync(owner, "Removed project");
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/projects/{removed}")).StatusCode);

        HttpClient admin = await CreateAdminClientAsync();
        HttpResponseMessage response = await admin.GetAsync("/api/admin/projects?page=1&pageSize=100");
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Guid[] ids = payload.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToArray();
        Assert.Contains(keep, ids);
        Assert.DoesNotContain(removed, ids);
    }

    [Fact]
    public async Task NonAdmin_CannotUseAdminProjectListing()
    {
        (HttpClient member, _, _) = await RegisterUserAsync();

        HttpResponseMessage response = await member.GetAsync("/api/admin/projects");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserProjectList_StillReturnsOnlyOwnMemberships()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient outsider, _, _) = await RegisterUserAsync();
        Guid ownProject = await CreateProjectAsync(owner, "Owned");
        Guid foreignProject = await CreateProjectAsync(outsider, "Foreign");

        HttpResponseMessage response = await owner.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Guid[] ids = payload.RootElement.EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToArray();

        Assert.Contains(ownProject, ids);
        Assert.DoesNotContain(foreignProject, ids);
    }

    private const string Password = "Password1";

    private async Task<(HttpClient Client, Guid UserId, string UserName)> RegisterUserAsync()
    {
        HttpClient client = factory.CreateClient();
        string suffix = Guid.NewGuid().ToString("N")[..8];
        string userName = $"user{suffix}";

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"user{suffix}@test.local",
            userName,
            password = Password,
            displayName = "Test User"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string token = payload.RootElement.GetProperty("accessToken").GetString()!;
        Guid userId = payload.RootElement.GetProperty("user").GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, userId, userName);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        HttpClient client = factory.CreateClient();
        string suffix = Guid.NewGuid().ToString("N")[..8];
        string userName = $"admin{suffix}";
        HttpResponseMessage register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"{userName}@test.local",
            userName,
            password = Password,
            displayName = "Admin User"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        using JsonDocument registered = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        Guid userId = registered.RootElement.GetProperty("user").GetProperty("id").GetGuid();

        HttpClient superAdmin = factory.CreateClient();
        HttpResponseMessage superLogin = await superAdmin.PostAsJsonAsync("/api/auth/login", new
        {
            userNameOrEmail = "superadmin",
            password = "SuperAdminPassword1"
        });
        Assert.Equal(HttpStatusCode.OK, superLogin.StatusCode);
        using JsonDocument superSession = JsonDocument.Parse(await superLogin.Content.ReadAsStringAsync());
        superAdmin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            superSession.RootElement.GetProperty("accessToken").GetString()!);

        HttpResponseMessage roles = await superAdmin.PutAsJsonAsync(
            $"/api/admin/users/{userId}/roles",
            new { roles = new[] { "Member", "Admin" } });
        Assert.Equal(HttpStatusCode.OK, roles.StatusCode);

        HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userNameOrEmail = userName,
            password = Password
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using JsonDocument session = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            session.RootElement.GetProperty("accessToken").GetString()!);
        return client;
    }

    private static async Task<Guid> CreateProjectAsync(HttpClient client, string name)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/projects", new
        {
            name,
            description = (string?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
    }
}
