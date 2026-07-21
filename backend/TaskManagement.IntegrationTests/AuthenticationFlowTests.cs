using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManagement.IntegrationTests;

// Covers the register/login/JWT flow end to end: a registered user can log in with
// the same credentials and reach protected endpoints, while missing, malformed or
// tampered tokens and wrong credentials are rejected with 401.
public sealed class AuthenticationFlowTests(TaskManagementApiFactory factory)
    : IClassFixture<TaskManagementApiFactory>
{
    [Fact]
    public async Task Register_ThenLogin_ReturnsTokenThatReachesProtectedEndpoint()
    {
        HttpClient client = factory.CreateClient();
        (string userName, string password) = await RegisterAsync(client);

        HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userNameOrEmail = userName,
            password
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using JsonDocument payload = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        string token = payload.RootElement.GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage projects = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, projects.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        HttpClient client = factory.CreateClient();
        (string userName, _) = await RegisterAsync(client);

        HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userNameOrEmail = userName,
            password = "WrongPassword1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTamperedToken_ReturnsUnauthorized()
    {
        HttpClient client = factory.CreateClient();
        (string userName, string password) = await RegisterAsync(client);

        HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            userNameOrEmail = userName,
            password
        });
        using JsonDocument payload = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        string token = payload.RootElement.GetProperty("accessToken").GetString()!;

        // The signature's FIRST character is mutated, not the last one: a 32-byte
        // HS256 signature is 43 base64url characters, so the final character's two
        // low bits are padding and are discarded when decoding. Swapping 'a' for 'b'
        // there leaves the decoded signature byte-identical and the token valid,
        // which made this test fail whenever the signature happened to end in a/b.
        int signatureStart = token.LastIndexOf('.') + 1;
        char original = token[signatureStart];
        string tampered = string.Concat(
            token.AsSpan(0, signatureStart),
            (original == 'A' ? 'B' : 'A').ToString(),
            token.AsSpan(signatureStart + 1));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        HttpResponseMessage response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<(string UserName, string Password)> RegisterAsync(HttpClient client)
    {
        string suffix = Guid.NewGuid().ToString("N")[..8];
        string userName = $"user{suffix}";
        const string password = "Password1";

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"{userName}@test.local",
            userName,
            password,
            displayName = "Test User"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (userName, password);
    }
}
