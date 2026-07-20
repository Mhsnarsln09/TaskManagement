using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagement.IntegrationTests;

public sealed class PostMvpTests(TaskManagementApiFactory factory) : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task LiveHealth_IsPublicAndHealthy()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Request_WithCorrelationId_EchoesItInResponse()
    {
        HttpClient client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", "integration-correlation-id");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal("integration-correlation-id", response.Headers.GetValues("X-Correlation-ID").Single());
    }

    [Fact]
    public async Task RefreshToken_RotatesAndReuseRevokesTheFamily()
    {
        HttpClient client = factory.CreateClient();
        string suffix = Guid.NewGuid().ToString("N")[..8];
        HttpResponseMessage register = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"refresh{suffix}@test.local",
            userName = $"refresh{suffix}",
            password = "Password1",
            displayName = "Refresh Test"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        using JsonDocument firstPayload = JsonDocument.Parse(await register.Content.ReadAsStringAsync());
        string firstToken = firstPayload.RootElement.GetProperty("refreshToken").GetString()!;

        HttpResponseMessage rotate = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = firstToken });
        Assert.Equal(HttpStatusCode.OK, rotate.StatusCode);
        using JsonDocument secondPayload = JsonDocument.Parse(await rotate.Content.ReadAsStringAsync());
        string secondToken = secondPayload.RootElement.GetProperty("refreshToken").GetString()!;
        Assert.NotEqual(firstToken, secondToken);

        HttpResponseMessage reuse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = firstToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        HttpResponseMessage revokedFamily = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = secondToken });
        Assert.Equal(HttpStatusCode.Unauthorized, revokedFamily.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_ConcurrentRotation_AllowsOnlyOneAndRevokesTheFamily()
    {
        HttpClient firstClient = factory.CreateClient();
        HttpClient secondClient = factory.CreateClient();
        (string _, string refreshToken) = await RegisterAsync(firstClient, "concurrent");

        Task<HttpResponseMessage> firstRotation = firstClient.PostAsJsonAsync(
            "/api/auth/refresh", new { refreshToken });
        Task<HttpResponseMessage> secondRotation = secondClient.PostAsJsonAsync(
            "/api/auth/refresh", new { refreshToken });

        HttpResponseMessage[] responses = await Task.WhenAll(firstRotation, secondRotation);

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Unauthorized);

        HttpResponseMessage successfulRotation = responses.Single(
            response => response.StatusCode == HttpStatusCode.OK);
        using JsonDocument payload = JsonDocument.Parse(
            await successfulRotation.Content.ReadAsStringAsync());
        string replacementToken = payload.RootElement.GetProperty("refreshToken").GetString()!;

        HttpResponseMessage revokedFamily = await firstClient.PostAsJsonAsync(
            "/api/auth/refresh", new { refreshToken = replacementToken });
        Assert.Equal(HttpStatusCode.Unauthorized, revokedFamily.StatusCode);
    }

    [Fact]
    public async Task SignalRNegotiate_WithQueryStringToken_IsAuthorized()
    {
        HttpClient client = factory.CreateClient();
        (string accessToken, _) = await RegisterAsync(client, "signalr");

        HttpResponseMessage response = await client.PostAsync(
            $"/hubs/notifications/negotiate?negotiateVersion=1&access_token={Uri.EscapeDataString(accessToken)}",
            content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CorsPreflight_FromConfiguredFrontendOrigin_IsAllowed()
    {
        HttpClient client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(
            "http://localhost:5173",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    private async Task<(string AccessToken, string RefreshToken)> RegisterAsync(
        HttpClient client,
        string prefix)
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"{prefix}{suffix}@test.local",
            userName = $"{prefix}{suffix}",
            password = "Password1",
            displayName = "Post MVP Test"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using JsonDocument payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (
            payload.RootElement.GetProperty("accessToken").GetString()!,
            payload.RootElement.GetProperty("refreshToken").GetString()!);
    }
}
