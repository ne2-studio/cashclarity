using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;

namespace CashClarity.AcceptanceTests;

[Collection(AcceptanceCollection.Name)]
public class McpTests(AcceptanceFixture fixture)
{
    [Fact]
    public async Task Mcp_endpoint_challenges_anonymous_clients_with_the_oidc_authority()
    {
        using var response = await fixture.Client.SendAsync(JsonRpc("tools/list"));
        using var metadata = await fixture.Client.GetAsync("/.well-known/oauth-protected-resource/mcp");
        using var json = JsonDocument.Parse(await metadata.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("resource_metadata=", response.Headers.WwwAuthenticate.ToString());
        Assert.Equal("http://fake-oidc:5000", json.RootElement.GetProperty("authorization_servers")[0].GetString());
    }

    [Fact]
    public async Task Mcp_tools_accept_the_same_oidc_access_token_as_the_rest_api()
    {
        using var tokenClient = fixture.CreateOidcTokenClient();
        var accessToken = await tokenClient.GetAccessTokenAsync(AcceptanceFixture.OidcUserKey);

        using var workspace = await CallToolAsync(accessToken, "create_workspace", new { name = "MCP" });
        var workspaceId = workspace.RootElement.GetProperty("id").GetString();
        using var accounts = await CallToolAsync(accessToken, "list_accounts", new { workspaceId });

        Assert.Contains(accounts.RootElement.EnumerateArray(), a =>
            a.GetProperty("type").GetString() == "main" &&
            a.GetProperty("userId").GetString() == AcceptanceFixture.OidcUserSub);
    }

    private async Task<JsonDocument> CallToolAsync(string accessToken, string tool, object arguments)
    {
        var request = JsonRpc("tools/call", new { name = tool, arguments });
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await fixture.Client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var result = JsonDocument.Parse(ExtractJson(body));
        var toolResult = result.RootElement.GetProperty("result");
        Assert.False(toolResult.TryGetProperty("isError", out var isError) && isError.GetBoolean(), body);
        return JsonDocument.Parse(toolResult.GetProperty("content")[0].GetProperty("text").GetString()!);
    }

    private static HttpRequestMessage JsonRpc(string method, object? @params = null)
    {
        var payload = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method, @params });
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        request.Headers.Add("MCP-Protocol-Version", "2025-06-18");
        return request;
    }

    /// <summary>Streamable HTTP may answer as plain JSON or as a single server-sent event.</summary>
    private static string ExtractJson(string body) =>
        body.TrimStart().StartsWith('{')
            ? body
            : body.Split('\n').Single(line => line.StartsWith("data:"))["data:".Length..].Trim();
}
