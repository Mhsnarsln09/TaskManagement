using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagement.IntegrationTests;

// Covers docs/tasks/10-mvp-hardening.md B10-07: notifications carry projectId/taskItemId
// and a structured type for navigation, the unread count is a total (not bound to the
// first page), and "mark all read" is a single server-side idempotent operation.
public sealed class NotificationContractTests(TaskManagementApiFactory factory)
    : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task Notifications_CarryProjectAndTaskAndType_ForNavigation()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient member, Guid memberId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        await AddMemberAsync(owner, projectId, memberId);
        Guid taskId = await CreateTaskAsync(owner, projectId, memberId);

        HttpResponseMessage list = await member.GetAsync("/api/notifications?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await list.Content.ReadAsStringAsync());

        JsonElement item = payload.RootElement.GetProperty("items").EnumerateArray().First();
        Assert.Equal(projectId, item.GetProperty("projectId").GetGuid());
        Assert.Equal(taskId, item.GetProperty("taskItemId").GetGuid());
        Assert.Equal("TaskAssigned", item.GetProperty("type").GetString());
        Assert.False(item.GetProperty("isRead").GetBoolean());
    }

    [Fact]
    public async Task UnreadCount_CountsAllNotifications_NotJustTheFirstPage()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient member, Guid memberId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        await AddMemberAsync(owner, projectId, memberId);

        // 21 assignments -> 21 notifications, more than one page of 20.
        for (int i = 0; i < 21; i++)
        {
            await CreateTaskAsync(owner, projectId, memberId);
        }

        HttpResponseMessage count = await member.GetAsync("/api/notifications/unread-count");
        Assert.Equal(HttpStatusCode.OK, count.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await count.Content.ReadAsStringAsync());
        Assert.Equal(21, payload.RootElement.GetProperty("unreadCount").GetInt32());
    }

    [Fact]
    public async Task MarkAllRead_ClearsEveryNotification_AndIsIdempotent()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient member, Guid memberId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        await AddMemberAsync(owner, projectId, memberId);
        for (int i = 0; i < 25; i++)
        {
            await CreateTaskAsync(owner, projectId, memberId);
        }

        HttpResponseMessage markAll = await member.PutAsJsonAsync("/api/notifications/read-all", new { });
        Assert.Equal(HttpStatusCode.NoContent, markAll.StatusCode);

        Assert.Equal(0, await UnreadCountAsync(member));

        // Idempotent: a second mark-all still succeeds and the count stays zero.
        HttpResponseMessage again = await member.PutAsJsonAsync("/api/notifications/read-all", new { });
        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);
        Assert.Equal(0, await UnreadCountAsync(member));
    }

    private const string Password = "Password1";

    private static async Task<int> UnreadCountAsync(HttpClient client)
    {
        HttpResponseMessage response = await client.GetAsync("/api/notifications/unread-count");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement.GetProperty("unreadCount").GetInt32();
    }

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

    private static async Task AddMemberAsync(HttpClient owner, Guid projectId, Guid userId)
    {
        HttpResponseMessage response = await owner.PostAsJsonAsync(
            $"/api/projects/{projectId}/members",
            new { userId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task<Guid> CreateTaskAsync(HttpClient client, Guid projectId, Guid assigneeUserId)
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
