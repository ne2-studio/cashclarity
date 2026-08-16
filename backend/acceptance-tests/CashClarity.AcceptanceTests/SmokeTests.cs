using System.Net;
using System.Text.Json;
using Xunit;

namespace CashClarity.AcceptanceTests;

[Collection(AcceptanceCollection.Name)]
public class SmokeTests(AcceptanceFixture fixture)
{
    [Fact]
    public async Task Health_is_public_and_reports_ok()
    {
        using var response = await fixture.Client.GetAsync("/server/health");
        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Protected_routes_reject_anonymous_requests()
    {
        using var response = await fixture.Client.GetAsync("/server/accounts");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
