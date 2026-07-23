using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagement.IntegrationTests;

// Covers docs/tasks/10-mvp-hardening.md B10-06: task responses carry an opaque version
// token and an update that sends a stale token is rejected with 409 instead of silently
// overwriting a newer version. Two clients that both loaded the same task stand in for
// the classic lost-update race.
public sealed class OptimisticConcurrencyTests(TaskManagementApiFactory factory)
    : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task StaleUpdate_IsRejected_AndDoesNotOverwriteNewerData()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);

        (Guid taskId, string version) = await CreateTaskAsync(owner, projectId);

        // First client edits with the version both clients loaded; this advances it.
        HttpResponseMessage first = await owner.PutAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{taskId}",
            UpdatePayload(title: "First client wins", version: version));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        using JsonDocument firstDoc = JsonDocument.Parse(await first.Content.ReadAsStringAsync());
        string newVersion = firstDoc.RootElement.GetProperty("version").GetString()!;
        Assert.NotEqual(version, newVersion);

        // Second client still holds the original (now stale) version.
        HttpResponseMessage second = await owner.PutAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{taskId}",
            UpdatePayload(title: "Second client overwrites", version: version));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        // The winning edit is intact; the stale write did not clobber it.
        HttpResponseMessage current = await owner.GetAsync($"/api/projects/{projectId}/tasks/{taskId}");
        using JsonDocument currentDoc = JsonDocument.Parse(await current.Content.ReadAsStringAsync());
        Assert.Equal("First client wins", currentDoc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Update_WithFreshVersion_Succeeds()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        (Guid taskId, string version) = await CreateTaskAsync(owner, projectId);

        HttpResponseMessage response = await owner.PutAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{taskId}",
            UpdatePayload(title: "Edited", version: version));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private const string Password = "Password1";

    private static object UpdatePayload(string title, string version) => new
    {
        title,
        description = (string?)null,
        status = "InProgress",
        priority = "Medium",
        dueDate = (string?)null,
        assigneeUserId = (Guid?)null,
        version
    };

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

    private static async Task<(Guid TaskId, string Version)> CreateTaskAsync(HttpClient client, Guid projectId)
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
        return (
            payload.RootElement.GetProperty("id").GetGuid(),
            payload.RootElement.GetProperty("version").GetString()!);
    }
}
