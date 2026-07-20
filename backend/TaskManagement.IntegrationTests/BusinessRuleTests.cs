using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Authentication;
using TaskManagement.Infrastructure.Identity;

namespace TaskManagement.IntegrationTests;

// Covers the authorization and business rules from docs/tasks/05-business-rules.md
// end to end: membership scoping, 404 for non-members, task delete restrictions,
// assignee membership, the completed-task reopen policy and duplicate memberships.
public sealed class BusinessRuleTests(TaskManagementApiFactory factory)
    : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task ListProjects_DoesNotIncludeProjectsTheUserIsNotMemberOf()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient outsider, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);

        HttpResponseMessage response = await outsider.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain(
            payload.RootElement.EnumerateArray(),
            project => project.GetProperty("id").GetGuid() == projectId);
    }

    [Fact]
    public async Task GetTask_AsNonMember_ReturnsNotFound()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient outsider, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);

        HttpResponseMessage response = await outsider.GetAsync($"/api/projects/{projectId}/tasks/{taskId}");

        // 404 instead of 403 so non-members cannot probe which project ids exist.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_AsProjectMember_ReturnsForbidden()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient member, Guid memberId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);
        await AddMemberAsync(owner, projectId, memberId);

        HttpResponseMessage response = await member.DeleteAsync($"/api/projects/{projectId}/tasks/{taskId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_AsProjectManagerOwningTheProject_Succeeds()
    {
        (HttpClient _, Guid managerId, string managerUserName) = await RegisterUserAsync();
        await AddToRoleAsync(managerId, ApplicationRoles.ProjectManager);
        // The registration token predates the role; log in again so the token
        // actually carries the ProjectManager role claim.
        HttpClient manager = await LoginAsync(managerUserName);
        Guid projectId = await CreateProjectAsync(manager);
        Guid taskId = await CreateTaskAsync(manager, projectId);

        HttpResponseMessage response = await manager.DeleteAsync($"/api/projects/{projectId}/tasks/{taskId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_AsOwnerWithoutProjectManagerRole_ReturnsForbidden()
    {
        // Registration only grants the Member role; owning the project is not
        // enough to delete tasks.
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);

        HttpResponseMessage response = await owner.DeleteAsync($"/api/projects/{projectId}/tasks/{taskId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithNonMemberAssignee_ReturnsValidationError()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (_, Guid outsiderId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);

        HttpResponseMessage response = await owner.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            title = "Task",
            description = (string?)null,
            priority = "Medium",
            dueDate = (string?)null,
            assigneeUserId = outsiderId
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("assigneeUserId", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UpdateTask_CompletedTaskWithoutReopen_ReturnsConflict()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);
        await UpdateTaskStatusAsync(owner, projectId, taskId, "Task", "Completed");

        HttpResponseMessage response = await owner.PutAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{taskId}",
            new
            {
                title = "Edited while completed",
                description = (string?)null,
                status = "Completed",
                priority = "Medium",
                dueDate = (string?)null,
                assigneeUserId = (Guid?)null
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_ReopeningCompletedTask_AllowsEditing()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        Guid taskId = await CreateTaskAsync(owner, projectId);
        await UpdateTaskStatusAsync(owner, projectId, taskId, "Task", "Completed");

        HttpResponseMessage response = await owner.PutAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{taskId}",
            new
            {
                title = "Reopened task",
                description = (string?)null,
                status = "InProgress",
                priority = "Medium",
                dueDate = (string?)null,
                assigneeUserId = (Guid?)null
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("InProgress", payload.RootElement.GetProperty("status").GetString());
        Assert.Equal("Reopened task", payload.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task AddMember_Twice_ReturnsConflict()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (_, Guid memberId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        await AddMemberAsync(owner, projectId, memberId);

        HttpResponseMessage response = await owner.PostAsJsonAsync(
            $"/api/projects/{projectId}/members",
            new { userId = memberId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddMember_AsNonOwnerMember_ReturnsForbidden()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (HttpClient member, Guid memberId, _) = await RegisterUserAsync();
        (_, Guid otherId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        await AddMemberAsync(owner, projectId, memberId);

        HttpResponseMessage response = await member.PostAsJsonAsync(
            $"/api/projects/{projectId}/members",
            new { userId = otherId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RemoveMember_Owner_ReturnsConflict()
    {
        (HttpClient owner, Guid ownerId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);

        HttpResponseMessage response = await owner.DeleteAsync($"/api/projects/{projectId}/members/{ownerId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RemoveMember_WithOpenAssignedTasks_ReturnsConflict()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (_, Guid memberId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        await AddMemberAsync(owner, projectId, memberId);

        HttpResponseMessage createTask = await owner.PostAsJsonAsync($"/api/projects/{projectId}/tasks", new
        {
            title = "Assigned task",
            description = (string?)null,
            priority = "Medium",
            dueDate = (string?)null,
            assigneeUserId = memberId
        });
        Assert.Equal(HttpStatusCode.Created, createTask.StatusCode);

        HttpResponseMessage response = await owner.DeleteAsync($"/api/projects/{projectId}/members/{memberId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RemoveMember_WithoutAssignedTasks_Succeeds()
    {
        (HttpClient owner, _, _) = await RegisterUserAsync();
        (_, Guid memberId, _) = await RegisterUserAsync();
        Guid projectId = await CreateProjectAsync(owner);
        await AddMemberAsync(owner, projectId, memberId);

        HttpResponseMessage response = await owner.DeleteAsync($"/api/projects/{projectId}/members/{memberId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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

    private async Task<HttpClient> LoginAsync(string userName)
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userNameOrEmail = userName,
            password = Password
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string token = payload.RootElement.GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // Roles are assigned out of band (registration always yields Member), so the
    // test elevates the user directly; the caller must log in again for a token
    // that carries the new role claim.
    private async Task AddToRoleAsync(Guid userId, string role)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser user = (await userManager.FindByIdAsync(userId.ToString()))!;

        IdentityResult result = await userManager.AddToRoleAsync(user, role);
        Assert.True(result.Succeeded);
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

    private static async Task AddMemberAsync(HttpClient owner, Guid projectId, Guid userId)
    {
        HttpResponseMessage response = await owner.PostAsJsonAsync(
            $"/api/projects/{projectId}/members",
            new { userId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static async Task UpdateTaskStatusAsync(
        HttpClient client,
        Guid projectId,
        Guid taskId,
        string title,
        string status)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/projects/{projectId}/tasks/{taskId}",
            new
            {
                title,
                description = (string?)null,
                status,
                priority = "Medium",
                dueDate = (string?)null,
                assigneeUserId = (Guid?)null
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
