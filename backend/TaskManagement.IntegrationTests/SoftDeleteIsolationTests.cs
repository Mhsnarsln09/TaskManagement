using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace TaskManagement.IntegrationTests;

// Covers docs/tasks/10-mvp-hardening.md B10-01: once a project is soft-deleted, none
// of its sub-resources (tasks, statistics, comments, attachments) may be reached
// through the normal API — not by a former member holding known ids, and not by an
// Admin whose system role otherwise bypasses membership. Membership is resolved
// through the soft-delete-filtered Projects set, so deletion closes every
// project-scoped path at once.
public sealed class SoftDeleteIsolationTests(TaskManagementApiFactory factory)
    : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task FormerMember_CannotReachAnySubResource_OfSoftDeletedProject()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);
        await AddCommentAsync(owner, projectId, taskId, "before deletion");
        HttpResponseMessage upload = await UploadAsync(owner, projectId, taskId, "notes.txt", "text/plain", "hello");
        using JsonDocument uploaded = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        Guid attachmentId = uploaded.RootElement.GetProperty("id").GetGuid();

        // The owner keeps the same authenticated client after deletion, standing in for
        // a former member who still holds valid credentials and known resource ids.
        HttpResponseMessage delete = await owner.DeleteAsync($"/api/projects/{projectId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/projects/{projectId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/projects/{projectId}/tasks")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/projects/{projectId}/tasks/{taskId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/projects/{projectId}/statistics")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(CommentsUrl(projectId, taskId))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync(AttachmentsUrl(projectId, taskId))).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await owner.GetAsync($"{AttachmentsUrl(projectId, taskId)}/{attachmentId}/content")).StatusCode);

        // Writes are closed too, not just reads.
        HttpResponseMessage comment = await owner.PostAsJsonAsync(
            CommentsUrl(projectId, taskId),
            new { content = "after deletion" });
        Assert.Equal(HttpStatusCode.NotFound, comment.StatusCode);
    }

    [Fact]
    public async Task Admin_CannotReachSoftDeletedProject_ThroughNormalApi()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);

        HttpClient admin = await CreateAdminClientAsync();
        // Sanity check: while the project is active the Admin bypass reaches it.
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/projects/{projectId}/tasks")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/projects/{projectId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/projects/{projectId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/projects/{projectId}/tasks")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/projects/{projectId}/tasks/{taskId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/projects/{projectId}/statistics")).StatusCode);
    }

    private const string Password = "Password1";

    private static string CommentsUrl(Guid projectId, Guid taskId) =>
        $"/api/projects/{projectId}/tasks/{taskId}/comments";

    private static string AttachmentsUrl(Guid projectId, Guid taskId) =>
        $"/api/projects/{projectId}/tasks/{taskId}/attachments";

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

    // Promotes a fresh user to the Admin system role via the bootstrap SuperAdmin, then
    // signs in again because a role change revokes the user's existing tokens.
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

    private static async Task<Guid> CreateTaskAsync(HttpClient client, Guid projectId)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            title = "Task",
            description = (string?)null,
            priority = "Medium",
            dueDate = (string?)null,
            assigneeUserId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task AddCommentAsync(HttpClient client, Guid projectId, Guid taskId, string content)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(CommentsUrl(projectId, taskId), new { content });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> UploadAsync(
        HttpClient client,
        Guid projectId,
        Guid taskId,
        string fileName,
        string contentType,
        string content)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(fileContent, "file", fileName);

        return await client.PostAsync(AttachmentsUrl(projectId, taskId), form);
    }
}
