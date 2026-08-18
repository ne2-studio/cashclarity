using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace CashClarity.AcceptanceTests;

public sealed class FakeOidcTokenClient : IDisposable
{
    private readonly HttpClient httpClient;

    public FakeOidcTokenClient(Uri fakeOidcBaseAddress)
    {
        httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = fakeOidcBaseAddress,
        };
    }

    public async Task<string> GetAccessTokenAsync(string userKey)
    {
        var query = "response_type=code" +
            $"&client_id={AcceptanceFixture.OidcClientId}" +
            $"&redirect_uri={Uri.EscapeDataString(AcceptanceFixture.OidcRedirectUri)}" +
            "&state=acceptance-tests" +
            $"&user={userKey}";

        using var authorizeResponse = await httpClient.GetAsync($"/authorize/select?{query}");
        if (authorizeResponse.StatusCode != HttpStatusCode.Found)
        {
            throw new InvalidOperationException($"fake-oidc did not issue an authorization code: {authorizeResponse.StatusCode}");
        }

        var redirectUri = authorizeResponse.Headers.Location
            ?? throw new InvalidOperationException("fake-oidc redirected without Location");
        var code = GetQueryParam(redirectUri, "code")
            ?? throw new InvalidOperationException($"fake-oidc redirect had no code: {redirectUri}");

        using var tokenResponse = await httpClient.PostAsync("/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = AcceptanceFixture.OidcClientId,
            ["redirect_uri"] = AcceptanceFixture.OidcRedirectUri,
        }));
        tokenResponse.EnsureSuccessStatusCode();

        var payload = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>()
            ?? throw new InvalidOperationException("fake-oidc returned an empty token response");
        return payload.AccessToken;
    }

    public void Dispose() => httpClient.Dispose();

    private static string? GetQueryParam(Uri uri, string name) =>
        uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(parts => parts[0] == name)
            .Select(parts => Uri.UnescapeDataString(parts.ElementAtOrDefault(1) ?? string.Empty))
            .FirstOrDefault();

    private record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("id_token")] string IdToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
