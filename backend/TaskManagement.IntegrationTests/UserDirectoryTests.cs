using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagement.IntegrationTests;

// The member picker needs to resolve people by name; these tests pin the exposure
// policy of that lookup (no e-mail, no roles) and its guard rails.
public sealed class UserDirectoryTests(TaskManagementApiFactory factory)
    : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task SearchUsers_WithoutToken_ReturnsUnauthorized()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/users?search=ayse");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SearchUsers_MatchesDisplayNameAndUserName()
    {
        HttpClient client = factory.CreateClient();
        string suffix = Guid.NewGuid().ToString("N")[..8];
        await RegisterAndSignInAsync(client, $"picker{suffix}", $"Zeynep Arslan {suffix}");

        HttpResponseMessage byUserName = await client.GetAsync($"/api/users?search=picker{suffix}");
        HttpResponseMessage byDisplayName = await client.GetAsync($"/api/users?search=Arslan {suffix}");

        Assert.Equal(HttpStatusCode.OK, byUserName.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byDisplayName.StatusCode);
        Assert.Contains($"picker{suffix}", await byUserName.Content.ReadAsStringAsync());
        Assert.Contains($"picker{suffix}", await byDisplayName.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SearchUsers_DoesNotExposeEmailOrRoles()
    {
        HttpClient client = factory.CreateClient();
        string suffix = Guid.NewGuid().ToString("N")[..8];
        await RegisterAndSignInAsync(client, $"private{suffix}", "Gizli Kullanıcı");

        HttpResponseMessage response = await client.GetAsync($"/api/users?search=private{suffix}");
        string body = await response.Content.ReadAsStringAsync();

        using JsonDocument payload = JsonDocument.Parse(body);
        JsonElement first = payload.RootElement.EnumerateArray().First();
        Assert.True(first.TryGetProperty("id", out _));
        Assert.True(first.TryGetProperty("userName", out _));
        Assert.True(first.TryGetProperty("displayName", out _));
        Assert.False(first.TryGetProperty("email", out _));
        Assert.False(first.TryGetProperty("roles", out _));
        Assert.DoesNotContain("@test.local", body);
    }

    [Fact]
    public async Task SearchUsers_WithTooShortTerm_ReturnsValidationError()
    {
        HttpClient client = factory.CreateClient();
        await RegisterAndSignInAsync(client, $"short{Guid.NewGuid():N}"[..12], null);

        HttpResponseMessage response = await client.GetAsync("/api/users?search=a");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchUsers_WithLimitAboveCeiling_ReturnsValidationError()
    {
        HttpClient client = factory.CreateClient();
        await RegisterAndSignInAsync(client, $"cap{Guid.NewGuid():N}"[..12], null);

        // The ceiling is enforced by the validator, so an oversized limit is a 400
        // rather than a silently clamped success.
        HttpResponseMessage response = await client.GetAsync("/api/users?search=us&limit=500");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListMembers_IncludesUserSummaryForEachMember()
    {
        HttpClient client = factory.CreateClient();
        string suffix = Guid.NewGuid().ToString("N")[..8];
        await RegisterAndSignInAsync(client, $"owner{suffix}", "Proje Sahibi");

        HttpResponseMessage created = await client.PostAsJsonAsync("/api/projects", new
        {
            name = $"Üye isimleri {suffix}",
            description = (string?)null
        });
        using JsonDocument project = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        string projectId = project.RootElement.GetProperty("id").GetString()!;

        HttpResponseMessage members = await client.GetAsync($"/api/projects/{projectId}/members");
        using JsonDocument payload = JsonDocument.Parse(await members.Content.ReadAsStringAsync());
        JsonElement owner = payload.RootElement.EnumerateArray().Single();

        // The picker and the member table both render this summary instead of a GUID.
        Assert.Equal("Proje Sahibi", owner.GetProperty("user").GetProperty("displayName").GetString());
        Assert.Equal($"owner{suffix}", owner.GetProperty("user").GetProperty("userName").GetString());
        Assert.False(owner.GetProperty("user").TryGetProperty("email", out _));
    }

    // Registers a user and signs the client in as that user. Tests run in parallel,
    // so credentials stay local to the call instead of living in shared fields.
    private static async Task RegisterAndSignInAsync(
        HttpClient client,
        string userName,
        string? displayName)
    {
        const string password = "Password1";

        HttpResponseMessage registered = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"{userName}@test.local",
            userName,
            password,
            displayName
        });
        registered.EnsureSuccessStatusCode();

        HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userNameOrEmail = userName,
            password
        });
        using JsonDocument payload = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        string token = payload.RootElement.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
