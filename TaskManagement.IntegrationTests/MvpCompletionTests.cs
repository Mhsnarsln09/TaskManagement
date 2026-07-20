using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace TaskManagement.IntegrationTests;

// Covers the acceptance criteria of docs/tasks/06-mvp-completion.md: comments and
// attachments are closed to non-members, hostile file names are normalized rather
// than trusted, and statistics are correct for mixed statuses and for empty projects.
public sealed class MvpCompletionTests(TaskManagementApiFactory factory)
    : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task AddComment_AsMember_ReturnsCommentWithAuthorSummary()
    {
        (HttpClient owner, _, string userName) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);

        HttpResponseMessage response = await owner.PostAsJsonAsync(
            CommentsUrl(projectId, taskId),
            new { content = "Looks good to me." });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Looks good to me.", payload.RootElement.GetProperty("content").GetString());

        JsonElement author = payload.RootElement.GetProperty("author");
        Assert.Equal(userName, author.GetProperty("userName").GetString());
        // The author summary must not carry account details such as the e-mail.
        Assert.False(author.TryGetProperty("email", out _));
    }

    [Fact]
    public async Task ListComments_ReturnsOldestFirstAndIsPaged()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);

        await AddCommentAsync(owner, projectId, taskId, "first");
        await AddCommentAsync(owner, projectId, taskId, "second");
        await AddCommentAsync(owner, projectId, taskId, "third");

        HttpResponseMessage response = await owner.GetAsync($"{CommentsUrl(projectId, taskId)}?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(3, payload.RootElement.GetProperty("totalCount").GetInt32());

        string[] contents = payload.RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("content").GetString()!)
            .ToArray();
        Assert.Equal(["first", "second"], contents);
    }

    [Fact]
    public async Task ListComments_AsNonMember_ReturnsNotFound()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient outsider, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);
        await AddCommentAsync(owner, projectId, taskId, "internal note");

        HttpResponseMessage response = await outsider.GetAsync(CommentsUrl(projectId, taskId));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddComment_ToTaskOfAnotherProject_ReturnsNotFound()
    {
        // The caller is a member of their own project, so membership alone passes;
        // the task must additionally belong to the project in the route.
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient other, _, _) = await RegisterUserAsync();
        Guid foreignProjectId = await CreateProjectAsync(owner);
        Guid foreignTaskId = await CreateTaskAsync(owner, foreignProjectId);
        Guid ownProjectId = await CreateProjectAsync(other);

        HttpResponseMessage response = await other.PostAsJsonAsync(
            CommentsUrl(ownProjectId, foreignTaskId),
            new { content = "cross project" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadAttachment_ThenDownload_RoundTripsTheContent()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);

        HttpResponseMessage upload = await UploadAsync(owner, projectId, taskId, "notes.txt", "text/plain", "hello");

        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        Guid attachmentId = payload.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("notes.txt", payload.RootElement.GetProperty("fileName").GetString());
        Assert.Equal(5, payload.RootElement.GetProperty("sizeInBytes").GetInt64());
        // The physical name is internal and must never reach the client.
        Assert.False(payload.RootElement.TryGetProperty("storedFileName", out _));

        HttpResponseMessage download = await owner.GetAsync(
            $"{AttachmentsUrl(projectId, taskId)}/{attachmentId}/content");

        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("hello", await download.Content.ReadAsStringAsync());
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task DownloadAttachment_AsNonMember_ReturnsNotFound()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient outsider, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);

        HttpResponseMessage upload = await UploadAsync(owner, projectId, taskId, "secret.txt", "text/plain", "hello");
        using JsonDocument payload = JsonDocument.Parse(await upload.Content.ReadAsStringAsync());
        Guid attachmentId = payload.RootElement.GetProperty("id").GetGuid();

        // Knowing the attachment id is not authorization.
        HttpResponseMessage response = await outsider.GetAsync(
            $"{AttachmentsUrl(projectId, taskId)}/{attachmentId}/content");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UploadAttachment_WithTraversalFileName_StoresOnlyTheFileNamePart()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);

        HttpResponseMessage response = await UploadAsync(
            owner,
            projectId,
            taskId,
            "../../../etc/passwd.txt",
            "text/plain",
            "payload");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("passwd.txt", payload.RootElement.GetProperty("fileName").GetString());

        // Everything physically written stays a flat file inside the storage root.
        string[] written = Directory.GetFiles(factory.StorageRootPath, "*", SearchOption.AllDirectories);
        Assert.All(written, path => Assert.Equal(factory.StorageRootPath, Path.GetDirectoryName(path)));
        Assert.DoesNotContain(written, path => Path.GetFileName(path).Contains("passwd", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UploadAttachment_WithDisallowedExtension_ReturnsBadRequest()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);

        HttpResponseMessage response = await UploadAsync(
            owner,
            projectId,
            taskId,
            "payload.exe",
            "application/octet-stream",
            "MZ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("extension", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadAttachment_WithEmptyFile_ReturnsBadRequest()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);

        HttpResponseMessage response = await UploadAsync(owner, projectId, taskId, "empty.txt", "text/plain", "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadAttachment_AsNonMember_ReturnsNotFound()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient outsider, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);

        HttpResponseMessage response = await UploadAsync(
            outsider,
            projectId,
            taskId,
            "notes.txt",
            "text/plain",
            "hello");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetStatistics_ForEmptyProject_ReturnsZeroes()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);

        HttpResponseMessage response = await owner.GetAsync($"/api/projects/{projectId}/statistics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, payload.RootElement.GetProperty("totalTasks").GetInt32());
        // No division by zero: an empty project is 0% complete.
        Assert.Equal(0m, payload.RootElement.GetProperty("completionPercentage").GetDecimal());
    }

    [Fact]
    public async Task GetStatistics_CountsEachStatusAndCompletionPercentage()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);

        await CreateTaskAsync(owner, projectId);
        Guid inProgressTaskId = await CreateTaskAsync(owner, projectId);
        Guid completedTaskId = await CreateTaskAsync(owner, projectId);
        Guid cancelledTaskId = await CreateTaskAsync(owner, projectId);

        await SetStatusAsync(owner, projectId, inProgressTaskId, "InProgress");
        await SetStatusAsync(owner, projectId, completedTaskId, "Completed");
        await SetStatusAsync(owner, projectId, cancelledTaskId, "Cancelled");

        HttpResponseMessage response = await owner.GetAsync($"/api/projects/{projectId}/statistics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement statistics = payload.RootElement;

        Assert.Equal(4, statistics.GetProperty("totalTasks").GetInt32());
        Assert.Equal(1, statistics.GetProperty("todoTasks").GetInt32());
        Assert.Equal(1, statistics.GetProperty("inProgressTasks").GetInt32());
        Assert.Equal(1, statistics.GetProperty("completedTasks").GetInt32());
        Assert.Equal(1, statistics.GetProperty("cancelledTasks").GetInt32());
        Assert.Equal(0, statistics.GetProperty("overdueTasks").GetInt32());
        // Cancelled work stays in the denominator, so 1 of 4 tasks done is 25%.
        Assert.Equal(25m, statistics.GetProperty("completionPercentage").GetDecimal());
    }

    [Fact]
    public async Task GetStatistics_AsNonMember_ReturnsNotFound()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient outsider, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);

        HttpResponseMessage response = await outsider.GetAsync($"/api/projects/{projectId}/statistics");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private const string Password = "Password1";

    private static string CommentsUrl(Guid projectId, Guid taskId)
    {
        return $"/api/projects/{projectId}/tasks/{taskId}/comments";
    }

    private static string AttachmentsUrl(Guid projectId, Guid taskId)
    {
        return $"/api/projects/{projectId}/tasks/{taskId}/attachments";
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

    private static async Task SetStatusAsync(HttpClient client, Guid projectId, Guid taskId, string status)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{taskId}",
            new
            {
                title = "Task",
                description = (string?)null,
                status,
                priority = "Medium",
                dueDate = (string?)null,
                assigneeUserId = (Guid?)null
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
