using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using CashClarity.Api.Contracts;
using CashClarity.Api.Controllers;
using CashClarity.Api.Mcp;
using CashClarity.Api.Repositories;
using CashClarity.Api.Services;
using CashClarity.Api.Services.EnableBanking;
using CashClarity.Api.Tests.EnableBanking;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Xunit;

namespace CashClarity.Api.Tests;

public sealed class McpServerTests : IAsyncLifetime
{
    private const string Authority = "https://auth.example.test";
    private WebApplication app = null!;
    private readonly MockEnableBankingApi bankApi = new();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:Authority"] = Authority });

        var accounts = new List<AccountResponse>();
        var journalEntries = new List<JournalEntryResponse>();
        var bankMovements = new List<BankMovementResponse>();
        builder.Services.AddSingleton<IWorkspacesRepository, InMemoryWorkspacesRepository>();
        builder.Services.AddSingleton<IAccountsRepository>(new InMemoryAccountsRepository(accounts, journalEntries, bankMovements));
        builder.Services.AddSingleton<IJournalEntriesRepository>(new InMemoryJournalEntriesRepository(accounts, journalEntries, bankMovements));
        builder.Services.AddSingleton<IBankMovementsRepository>(new InMemoryBankMovementsRepository(bankMovements, accounts, journalEntries));
        builder.Services.AddSingleton<IBankMovementImportService, BankMovementImportService>();
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 10, 24, 10, 0, 0, TimeSpan.Zero));
        builder.Services.AddSingleton<IBankLinksRepository, InMemoryBankLinksRepository>();
        builder.Services.AddSingleton<IEnableBankingClient>(bankApi.CreateClient(clock));
        builder.Services.AddSingleton<TimeProvider>(clock);
        builder.Services.AddSingleton<IBankLinkService, BankLinkService>();
        builder.Services.AddCashClarityMcp();
        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestBearerHandler.Scheme;
                options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestBearerHandler>(TestBearerHandler.Scheme, _ => { })
            .AddCashClarityMcpChallenge(builder.Configuration);
        builder.Services.AddAuthorization();

        app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCashClarityMcp();
        await app.StartAsync();
    }

    public async Task DisposeAsync() => await app.DisposeAsync();

    [Fact]
    public void Every_rest_endpoint_has_an_mcp_tool()
    {
        var endpoints = typeof(BaseController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Count(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());
        var tools = McpServerSetup.ToolTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Count(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null);

        Assert.Equal(24, app.Services.GetServices<McpServerTool>().Count());
        Assert.Equal(24, endpoints);
        Assert.Equal(endpoints, tools);
    }

    [Fact]
    public async Task Lists_one_tool_per_endpoint_with_read_only_hints()
    {
        await using var client = await Connect("user-a");

        var tools = await client.ListToolsAsync();

        Assert.Equal(
            [
                "commit_bank_movement_import", "complete_bank_authorization", "create_account", "create_bank_movement",
                "create_journal_entry", "create_workspace", "delete_account", "delete_bank_movement", "delete_journal_entry",
                "get_bank_link", "health_check", "list_accounts", "list_bank_aspsps", "list_bank_movements",
                "list_journal_entries", "list_workspaces", "preview_bank_movement_import", "select_bank_account",
                "start_bank_authorization", "sync_bank_link", "unlink_bank", "update_account", "update_bank_movement",
                "update_journal_entry",
            ],
            tools.Select(t => t.Name).Order());
        var createAccountSchema = tools.Single(t => t.Name == "create_account").JsonSchema.GetRawText();
        Assert.Contains("\"uncategorized\"", createAccountSchema);
        Assert.All(tools.Where(t => t.Name.StartsWith("list_")), t => Assert.True(t.ProtocolTool.Annotations?.ReadOnlyHint));
        Assert.All(tools.Where(t => t.Name.StartsWith("delete_")), t => Assert.True(t.ProtocolTool.Annotations?.DestructiveHint));
    }

    [Fact]
    public async Task Anonymous_requests_are_challenged_with_protected_resource_metadata()
    {
        var http = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, McpServerSetup.Path)
        {
            Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        using var response = await http.SendAsync(request);
        using var metadata = await http.GetAsync("/.well-known/oauth-protected-resource/mcp");
        using var json = JsonDocument.Parse(await metadata.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("resource_metadata=", response.Headers.WwwAuthenticate.ToString());
        Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);
        Assert.Equal(Authority, json.RootElement.GetProperty("authorization_servers")[0].GetString());
    }

    [Fact]
    public async Task Space_reservation_journey_works_through_tools_and_is_scoped_to_the_user()
    {
        await using var client = await Connect("user-a");
        await using var otherUser = await Connect("user-b");
        var ws = await CreateWorkspace(client);
        var otherWs = await CreateWorkspace(otherUser);

        var accounts = await Call(client, "list_accounts", new() { ["workspaceId"] = ws });
        var main = accounts.EnumerateArray().Single(a => a.GetProperty("type").GetString() == "main");
        var space = await Call(client, "create_account", new() { ["workspaceId"] = ws, ["code"] = "1001", ["name"] = "Vacaciones", ["type"] = "space" });
        var entry = await Call(client, "create_journal_entry", new()
        {
            ["workspaceId"] = ws,
            ["date"] = "2026-10-24",
            ["description"] = "Reserva vacaciones",
            ["lines"] = new[]
            {
                new { accountId = space.GetProperty("id").GetString(), debit = 100m, credit = 0m },
                new { accountId = main.GetProperty("id").GetString(), debit = 0m, credit = 100m },
            },
        });
        await Call(client, "update_account", new() { ["workspaceId"] = ws, ["id"] = space.GetProperty("id").GetString(), ["name"] = "Viaje" });

        Assert.Equal("space", space.GetProperty("type").GetString());
        Assert.Equal(2, entry.GetProperty("lines").GetArrayLength());
        Assert.Contains((await Call(client, "list_accounts", new() { ["workspaceId"] = ws })).EnumerateArray(), a => a.GetProperty("name").GetString() == "Viaje");
        Assert.Single((await Call(client, "list_journal_entries", new() { ["workspaceId"] = ws })).EnumerateArray());
        Assert.Empty((await Call(otherUser, "list_journal_entries", new() { ["workspaceId"] = otherWs })).EnumerateArray());
        Assert.Single((await Call(client, "list_workspaces")).EnumerateArray());

        await Call(client, "delete_journal_entry", new() { ["workspaceId"] = ws, ["id"] = entry.GetProperty("id").GetString() });
        await Call(client, "delete_account", new() { ["workspaceId"] = ws, ["id"] = space.GetProperty("id").GetString() });
        Assert.Empty((await Call(client, "list_journal_entries", new() { ["workspaceId"] = ws })).EnumerateArray());
    }

    [Fact]
    public async Task Bank_movements_can_be_imported_reconciled_and_deleted()
    {
        await using var client = await Connect("user-a");
        var ws = await CreateWorkspace(client);

        var preview = await Call(client, "preview_bank_movement_import", new()
        {
            ["workspaceId"] = ws,
            ["csvText"] = "fecha;Concepto;cantidad\n2026-10-01;Nomina;1500\n2026-10-02;Luz;-45,30\n",
        });
        var commit = await Call(client, "commit_bank_movement_import", new()
        {
            ["workspaceId"] = ws,
            ["rows"] = preview.GetProperty("rows"),
            ["duplicatePolicy"] = "skip",
        });
        var manual = await Call(client, "create_bank_movement", new() { ["workspaceId"] = ws, ["date"] = "2026-10-03", ["description"] = "Cafe", ["amount"] = -2.5m });
        await Call(client, "update_bank_movement", new() { ["workspaceId"] = ws, ["id"] = manual.GetProperty("id").GetString(), ["isIdentified"] = true });
        await Call(client, "delete_bank_movement", new() { ["workspaceId"] = ws, ["id"] = commit.GetProperty("created")[0].GetProperty("id").GetString() });

        var movements = (await Call(client, "list_bank_movements", new() { ["workspaceId"] = ws })).EnumerateArray().ToList();
        Assert.Equal(2, preview.GetProperty("summary").GetProperty("valid").GetInt32());
        Assert.Equal(2, commit.GetProperty("created").GetArrayLength());
        Assert.Equal(2, movements.Count);
        Assert.Contains(movements, m => m.GetProperty("description").GetString() == "Cafe" && m.GetProperty("isIdentified").GetBoolean());
    }

    [Fact]
    public async Task Bank_account_can_be_linked_synced_twice_and_unlinked_through_tools()
    {
        await using var client = await Connect("user-a");
        var ws = await CreateWorkspace(client);
        bankApi.Transactions = [MockEnableBankingApi.Transaction("ref-1", "2026-10-20", "1500.00", "CRDT", "Nomina octubre")];

        var banks = await Call(client, "list_bank_aspsps", new() { ["workspaceId"] = ws, ["country"] = "ES" });
        await Call(client, "start_bank_authorization", new()
        {
            ["workspaceId"] = ws, ["aspspName"] = "CaixaBank", ["aspspCountry"] = "ES", ["psuType"] = "business", ["syncFrom"] = "2026-10-01",
        });
        var state = bankApi.Requests.Last(r => r.Uri.AbsolutePath == "/auth").JsonBody!["state"]!.GetValue<string>();
        var link = await Call(client, "complete_bank_authorization", new() { ["workspaceId"] = ws, ["code"] = "code-1", ["state"] = state });
        var first = await Call(client, "sync_bank_link", new() { ["workspaceId"] = ws });
        var second = await Call(client, "sync_bank_link", new() { ["workspaceId"] = ws });
        await Call(client, "unlink_bank", new() { ["workspaceId"] = ws });

        Assert.Contains(banks.EnumerateArray(), b => b.GetProperty("name").GetString() == "CaixaBank");
        Assert.Equal("active", link.GetProperty("status").GetString());
        Assert.Equal(1, first.GetProperty("created").GetArrayLength());
        Assert.Equal(0, second.GetProperty("created").GetArrayLength());
        Assert.Single((await Call(client, "list_bank_movements", new() { ["workspaceId"] = ws })).EnumerateArray());
        Assert.Equal("none", (await Call(client, "get_bank_link", new() { ["workspaceId"] = ws })).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Domain_errors_are_reported_as_tool_errors_with_the_rest_status()
    {
        await using var client = await Connect("user-a");
        var ws = await CreateWorkspace(client);

        var unbalanced = await client.CallToolAsync("create_journal_entry", new Dictionary<string, object?>
        {
            ["workspaceId"] = ws,
            ["date"] = "2026-10-24",
            ["lines"] = new[] { new { accountId = "a", debit = 10m, credit = 0m } },
        });
        var missing = await client.CallToolAsync("update_bank_movement", new Dictionary<string, object?>
        {
            ["workspaceId"] = ws,
            ["id"] = Guid.NewGuid().ToString(),
            ["description"] = "x",
        });

        Assert.True(unbalanced.IsError);
        Assert.StartsWith("400:", Text(unbalanced));
        Assert.True(missing.IsError);
        Assert.StartsWith("404:", Text(missing));
    }

    [Fact]
    public async Task Workspaces_of_other_users_are_reported_as_not_found()
    {
        await using var owner = await Connect("user-a");
        await using var intruder = await Connect("user-b");
        var ws = await CreateWorkspace(owner);

        var result = await intruder.CallToolAsync("list_accounts", new Dictionary<string, object?> { ["workspaceId"] = ws });
        var malformed = await owner.CallToolAsync("list_accounts", new Dictionary<string, object?> { ["workspaceId"] = "not-a-guid" });

        Assert.True(result.IsError);
        Assert.StartsWith("404:", Text(result));
        Assert.StartsWith("404:", Text(malformed));
    }

    [Theory]
    [InlineData("create_account", """{"code":"1","name":"x","type":"hucha"}""")]
    [InlineData("preview_bank_movement_import", "{}")]
    [InlineData("preview_bank_movement_import", """{"fileBase64":"%%%"}""")]
    public async Task Invalid_input_is_rejected(string tool, string arguments)
    {
        await using var client = await Connect("user-a");
        var input = JsonSerializer.Deserialize<Dictionary<string, object?>>(arguments)!;
        input["workspaceId"] = await CreateWorkspace(client);

        var result = await client.CallToolAsync(tool, input);

        Assert.True(result.IsError);
    }

    private static async Task<string> CreateWorkspace(McpClient client) =>
        (await Call(client, "create_workspace", new() { ["name"] = "Personal" })).GetProperty("id").GetString()!;

    private async Task<McpClient> Connect(string userId)
    {
        var http = app.GetTestClient();
        http.DefaultRequestHeaders.Authorization = new("Bearer", userId);
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(http.BaseAddress!, McpServerSetup.Path), TransportMode = HttpTransportMode.StreamableHttp },
            http,
            ownsHttpClient: true);
        return await McpClient.CreateAsync(transport);
    }

    private static async Task<JsonElement> Call(McpClient client, string tool, Dictionary<string, object?>? arguments = null)
    {
        var result = await client.CallToolAsync(tool, arguments ?? []);
        Assert.True(result.IsError is not true, Text(result));
        return JsonDocument.Parse(Text(result)).RootElement;
    }

    private static string Text(CallToolResult result) =>
        string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    /// <summary>Treats the bearer token as the user id, standing in for a validated OIDC access token.</summary>
    private sealed class TestBearerHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Scheme = "TestBearer";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var header = Request.Headers.Authorization.ToString();
            if (!header.StartsWith("Bearer ")) return Task.FromResult(AuthenticateResult.NoResult());

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, header["Bearer ".Length..])], Scheme);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
        }
    }
}
