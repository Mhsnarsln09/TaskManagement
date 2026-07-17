using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagement.IntegrationTests;

public sealed class ValidationTests(TaskManagementApiFactory factory)
    : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task Register_WithInvalidEmailAndWeakPassword_ReturnsFieldErrors()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            userName = "validuser",
            password = "short",
            displayName = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Dictionary<string, string[]> errors = await ReadErrorsAsync(response);
        Assert.Contains("Email", errors.Keys);
        Assert.Contains("Password", errors.Keys);
    }

    [Fact]
    public async Task Login_WithEmptyFields_ReturnsFieldErrors()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userNameOrEmail = "",
            password = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Dictionary<string, string[]> errors = await ReadErrorsAsync(response);
        Assert.Contains("UserNameOrEmail", errors.Keys);
        Assert.Contains("Password", errors.Keys);
    }

    [Fact]
    public async Task CreateProject_WithoutToken_ReturnsUnauthorized()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "Project",
            description = (string?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithEmptyName_ReturnsFieldErrors()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "",
            description = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Dictionary<string, string[]> errors = await ReadErrorsAsync(response);
        Assert.Contains("Name", errors.Keys);
    }

    [Fact]
    public async Task CreateTask_WithEmptyTitle_ReturnsFieldErrors()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();
        Guid projectId = await CreateProjectAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            title = "",
            description = (string?)null,
            priority = "Medium",
            dueDate = (string?)null,
            assigneeUserId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Dictionary<string, string[]> errors = await ReadErrorsAsync(response);
        Assert.Contains("Title", errors.Keys);
    }

    [Fact]
    public async Task ListTasks_WithInvalidQuery_ReturnsFieldErrors()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();
        Guid projectId = await CreateProjectAsync(client);

        HttpResponseMessage response = await client.GetAsync(
            $"/api/projects/{projectId}/tasks?page=0&pageSize=1000&sortBy=bogus");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Dictionary<string, string[]> errors = await ReadErrorsAsync(response);
        Assert.Contains("Page", errors.Keys);
        Assert.Contains("PageSize", errors.Keys);
        Assert.Contains("SortBy", errors.Keys);
    }

    [Fact]
    public async Task CreateTask_WithValidRequest_ReturnsCreated()
    {
        HttpClient client = await CreateAuthenticatedClientAsync();
        Guid projectId = await CreateProjectAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            title = "Valid task",
            description = (string?)null,
            priority = "Medium",
            dueDate = (string?)null,
            assigneeUserId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        HttpClient client = factory.CreateClient();
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
        string token = payload.RootElement.GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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

    private static async Task<Dictionary<string, string[]>> ReadErrorsAsync(HttpResponseMessage response)
    {
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement errors = payload.RootElement.GetProperty("errors");

        return errors.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value.EnumerateArray().Select(value => value.GetString()!).ToArray());
    }
}
