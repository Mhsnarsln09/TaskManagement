using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagement.IntegrationTests;

// Covers docs/tasks/10-mvp-hardening.md B10-02: the task authority matrix is centralised
// and every actor is exercised separately.
//
//  Actor        | create | edit fields | reassign | change own status | delete
//  ------------ | ------ | ----------- | -------- | ----------------- | ------
//  Owner        |  yes   |    yes      |   yes    |       yes         |  yes
//  Admin        |  yes   |    yes      |   yes    |       yes         |  yes
//  Assignee     |  no    |    no       |   no     |       yes         |  no
//  Other member |  no    |    no       |   no     |       no          |  no
public sealed class TaskAuthorizationTests(TaskManagementApiFactory factory)
    : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task Owner_CanCreateEditReassignAndDeleteTasks()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient memberClient, Guid memberId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        await AddMemberAsync(owner, projectId, memberId);
        _ = memberClient;

        Guid taskId = await CreateTaskAsync(owner, projectId);

        // Edit every field and reassign to the member.
        HttpResponseMessage edit = await owner.PutAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{taskId}",
            new
            {
                title = "Owner edited",
                description = "changed",
                status = "InProgress",
                priority = "High",
                dueDate = (string?)null,
                assigneeUserId = memberId
            });
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        using JsonDocument edited = JsonDocument.Parse(await edit.Content.ReadAsStringAsync());
        Assert.Equal("Owner edited", edited.RootElement.GetProperty("title").GetString());
        Assert.Equal(memberId, edited.RootElement.GetProperty("assigneeUserId").GetGuid());

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await owner.DeleteAsync($"/api/projects/{projectId}/tasks/{taskId}")).StatusCode);
    }

    [Fact]
    public async Task Admin_CanManageTasks_WithoutBeingAMember()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        HttpClient admin = await CreateAdminClientAsync();

        HttpResponseMessage create = await admin.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            title = "Admin task",
            description = (string?)null,
            priority = "Medium",
            dueDate = (string?)null,
            assigneeUserId = (Guid?)null
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using JsonDocument created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        Guid taskId = created.RootElement.GetProperty("id").GetGuid();

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await admin.DeleteAsync($"/api/projects/{projectId}/tasks/{taskId}")).StatusCode);
    }

    [Fact]
    public async Task Assignee_CanChangeOwnStatus_ButNotOtherFieldsOrLifecycle()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient assignee, Guid assigneeId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        await AddMemberAsync(owner, projectId, assigneeId);
        Guid taskId = await CreateTaskAsync(owner, projectId, assigneeId);

        // Status-only change: keeps every other field identical to the stored task.
        HttpResponseMessage status = await assignee.PutAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{taskId}",
            new
            {
                title = "Task",
                description = (string?)null,
                status = "InProgress",
                priority = "Medium",
                dueDate = (string?)null,
                assigneeUserId = assigneeId
            });
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);

        // Changing another field is forbidden.
        HttpResponseMessage rename = await assignee.PutAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{taskId}",
            new
            {
                title = "Renamed by assignee",
                description = (string?)null,
                status = "InProgress",
                priority = "Medium",
                dueDate = (string?)null,
                assigneeUserId = assigneeId
            });
        Assert.Equal(HttpStatusCode.Forbidden, rename.StatusCode);

        // Creating and deleting are forbidden for an assignee.
        HttpResponseMessage create = await assignee.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            title = "Assignee task",
            description = (string?)null,
            priority = "Medium",
            dueDate = (string?)null,
            assigneeUserId = (Guid?)null
        });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await assignee.DeleteAsync($"/api/projects/{projectId}/tasks/{taskId}")).StatusCode);
    }

    [Fact]
    public async Task OtherMember_CannotWriteTasks_ButCanReadAndComment()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient member, Guid memberId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        await AddMemberAsync(owner, projectId, memberId);
        // Task is assigned to the owner, so the plain member is not the assignee.
        Guid taskId = await CreateTaskAsync(owner, projectId);

        // Reads succeed.
        Assert.Equal(HttpStatusCode.OK, (await member.GetAsync($"/api/projects/{projectId}/tasks/{taskId}")).StatusCode);

        // Any task write is forbidden.
        HttpResponseMessage update = await member.PutAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{taskId}",
            new
            {
                title = "Task",
                description = (string?)null,
                status = "InProgress",
                priority = "Medium",
                dueDate = (string?)null,
                assigneeUserId = (Guid?)null
            });
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await member.DeleteAsync($"/api/projects/{projectId}/tasks/{taskId}")).StatusCode);

        // Collaboration (comments) remains open to any member.
        HttpResponseMessage comment = await member.PostAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{taskId}/comments",
            new { content = "A member note" });
        Assert.Equal(HttpStatusCode.Created, comment.StatusCode);
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

    private static async Task<Guid> CreateProjectAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "Test Project",
            description = (string?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task AddMemberAsync(HttpClient owner, Guid projectId, Guid userId)
    {
        HttpResponseMessage response = await owner.PostAsJsonAsync(
            $"/api/projects/{projectId}/members",
            new { userId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<Guid> CreateTaskAsync(HttpClient client, Guid projectId, Guid? assigneeUserId = null)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            title = "Task",
            description = (string?)null,
            priority = "Medium",
            dueDate = (string?)null,
            assigneeUserId
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
    }
}
