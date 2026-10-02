using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CashClarity.AcceptanceTests;

[Collection(AcceptanceCollection.Name)]
public class CriticalJourneyTests(AcceptanceFixture fixture)
{
    [Fact]
    public async Task Account_onboarding_journey_initializes_system_accounts_and_manages_a_user_space()
    {
        using var client = await CreateAuthenticatedClientAsync(AcceptanceFixture.OidcUserKey);
        var ws = await CreateWorkspaceAsync(client, "Principal acceptance");

        var initialAccounts = await GetArrayAsync(client, $"{ws}/accounts");
        Assert.Contains(initialAccounts, account =>
            account.GetProperty("code").GetString() == "0001" &&
            account.GetProperty("isSystem").GetBoolean());
        Assert.Contains(initialAccounts, account =>
            account.GetProperty("code").GetString() == "9999" &&
            account.GetProperty("isSystem").GetBoolean());

        var code = UniqueCode("RES");
        var account = await PostJsonAsync(client, $"{ws}/accounts", new
        {
            code,
            name = "Reserva IVA acceptance",
            type = "reserve",
            balance = 123.45m,
        });
        var accountId = AssertNonEmptyString(account, "id");
        Assert.Equal(code, account.GetProperty("code").GetString());
        Assert.Equal("acceptance-user", account.GetProperty("userId").GetString());

        var patchResponse = await client.PatchAsJsonAsync($"{ws}/accounts/{accountId}", new
        {
            name = "Reserva impuestos acceptance",
            balance = 200m,
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var updatedAccounts = await GetArrayAsync(client, $"{ws}/accounts");
        var updated = AssertSingleById(updatedAccounts, accountId);
        Assert.Equal("Reserva impuestos acceptance", updated.GetProperty("name").GetString());
        Assert.Equal(200m, updated.GetProperty("balance").GetDecimal());

        var deleteResponse = await client.DeleteAsync($"{ws}/accounts/{accountId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var finalAccounts = await GetArrayAsync(client, $"{ws}/accounts");
        Assert.DoesNotContain(finalAccounts, item => item.GetProperty("id").GetString() == accountId);
    }

    [Fact]
    public async Task Manual_accounting_journey_records_updates_and_removes_a_journal_entry()
    {
        using var client = await CreateAuthenticatedClientAsync(AcceptanceFixture.OidcUserKey);
        var ws = await CreateWorkspaceAsync(client, "Principal acceptance");
        var bankAccountId = await CreateAccountAsync(client, ws, UniqueCode("BNK"), "Banco acceptance", "main");
        var reserveAccountId = await CreateAccountAsync(client, ws, UniqueCode("TAX"), "Impuestos acceptance", "reserve");

        var entry = await PostJsonAsync(client, $"{ws}/journal-entries", new
        {
            date = "2026-08-18",
            description = "Provision impuestos acceptance",
            lines = new[]
            {
                new { accountId = reserveAccountId, debit = 300m, credit = 0m, description = "Dotacion" },
                new { accountId = bankAccountId, debit = 0m, credit = 300m, description = "Salida banco" },
            },
        });
        var entryId = AssertNonEmptyString(entry, "id");
        Assert.Equal(2, entry.GetProperty("lines").GetArrayLength());

        var listedEntry = AssertSingleById(await GetArrayAsync(client, $"{ws}/journal-entries"), entryId);
        Assert.Equal("Provision impuestos acceptance", listedEntry.GetProperty("description").GetString());

        var patchResponse = await client.PatchAsJsonAsync($"{ws}/journal-entries/{entryId}", new
        {
            date = "2026-08-19",
            description = "Provision impuestos ajustada",
            lines = new[]
            {
                new { accountId = reserveAccountId, debit = 250m, credit = 0m, description = "Dotacion ajustada" },
                new { accountId = bankAccountId, debit = 0m, credit = 250m, description = "Salida banco ajustada" },
            },
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var updatedEntry = AssertSingleById(await GetArrayAsync(client, $"{ws}/journal-entries"), entryId);
        Assert.Equal("Provision impuestos ajustada", updatedEntry.GetProperty("description").GetString());
        Assert.Contains(updatedEntry.GetProperty("lines").EnumerateArray(), line => line.GetProperty("debit").GetDecimal() == 250m);

        var deleteResponse = await client.DeleteAsync($"{ws}/journal-entries/{entryId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        Assert.DoesNotContain(await GetArrayAsync(client, $"{ws}/journal-entries"), item => item.GetProperty("id").GetString() == entryId);
    }

    [Fact]
    public async Task Bank_movement_reconciliation_journey_imports_identifies_links_and_removes_a_movement()
    {
        using var client = await CreateAuthenticatedClientAsync(AcceptanceFixture.OidcUserKey);
        var ws = await CreateWorkspaceAsync(client, "Principal acceptance");
        var bankAccountId = await CreateAccountAsync(client, ws, UniqueCode("BNK"), "Banco conciliacion acceptance", "main");
        var expenseAccountId = await CreateAccountAsync(client, ws, UniqueCode("EXP"), "Gasto acceptance", "expense");
        var entryId = await CreateJournalEntryAsync(client, ws, bankAccountId, expenseAccountId);

        var movement = await PostJsonAsync(client, $"{ws}/bank-movements", new
        {
            date = "2026-08-18",
            description = "Cargo tarjeta pendiente",
            amount = -42.50m,
        });
        var movementId = AssertNonEmptyString(movement, "id");
        Assert.False(movement.GetProperty("isIdentified").GetBoolean());

        var patchResponse = await client.PatchAsJsonAsync($"{ws}/bank-movements/{movementId}", new
        {
            description = "Cargo tarjeta conciliado",
            isIdentified = true,
            entityId = expenseAccountId,
            journalEntryId = entryId,
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var reconciled = AssertSingleById(await GetArrayAsync(client, $"{ws}/bank-movements"), movementId);
        Assert.True(reconciled.GetProperty("isIdentified").GetBoolean());
        Assert.Equal(expenseAccountId, reconciled.GetProperty("entityId").GetString());
        Assert.Equal(entryId, reconciled.GetProperty("journalEntryId").GetString());

        var deleteResponse = await client.DeleteAsync($"{ws}/bank-movements/{movementId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        Assert.DoesNotContain(await GetArrayAsync(client, $"{ws}/bank-movements"), item => item.GetProperty("id").GetString() == movementId);
    }

    [Fact]
    public async Task User_data_is_isolated_between_authenticated_callers()
    {
        using var primaryClient = await CreateAuthenticatedClientAsync(AcceptanceFixture.OidcUserKey);
        using var secondClient = await CreateAuthenticatedClientAsync(AcceptanceFixture.OidcSecondUserKey);

        var primaryWs = await CreateWorkspaceAsync(primaryClient, "Privado");
        var secondWs = await CreateWorkspaceAsync(secondClient, "Segundo");
        var code = UniqueCode("ISO");
        var accountId = await CreateAccountAsync(primaryClient, primaryWs, code, "Cuenta privada acceptance", "reserve");

        var primaryAccounts = await GetArrayAsync(primaryClient, $"{primaryWs}/accounts");
        Assert.Contains(primaryAccounts, account => account.GetProperty("id").GetString() == accountId);

        var secondAccounts = await GetArrayAsync(secondClient, $"{secondWs}/accounts");
        Assert.DoesNotContain(secondAccounts, account => account.GetProperty("code").GetString() == code);
        Assert.All(secondAccounts, account => Assert.Equal("second-acceptance-user", account.GetProperty("userId").GetString()));

        // The second user cannot reach the primary user's workspace at all.
        foreach (var resource in new[] { "accounts", "journal-entries", "bank-movements" })
        {
            using var response = await secondClient.GetAsync($"{primaryWs}/{resource}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        using var forbiddenWrite = await secondClient.PostAsJsonAsync($"{primaryWs}/accounts",
            new { code = UniqueCode("HAK"), name = "Intruso", type = "reserve", balance = 0m });
        Assert.Equal(HttpStatusCode.NotFound, forbiddenWrite.StatusCode);

        Assert.DoesNotContain(await GetArrayAsync(secondClient, "/server/workspaces"),
            w => w.GetProperty("id").GetString() == primaryWs.Split('/').Last());
    }

    [Fact]
    public async Task Workspace_journey_creates_lists_and_isolates_data_between_workspaces()
    {
        using var client = await CreateAuthenticatedClientAsync(AcceptanceFixture.OidcSecondUserKey);

        using var emptyName = await client.PostAsJsonAsync("/server/workspaces", new { name = "   " });
        Assert.Equal(HttpStatusCode.BadRequest, emptyName.StatusCode);

        var first = await CreateWorkspaceAsync(client, "  Uno  ");
        var second = await CreateWorkspaceAsync(client, "Dos");
        var listed = await GetArrayAsync(client, "/server/workspaces");
        Assert.Contains(listed, w => w.GetProperty("name").GetString() == "Uno");
        Assert.Contains(listed, w => w.GetProperty("name").GetString() == "Dos");

        // New workspaces start with their own system accounts.
        Assert.Equal(2, (await GetArrayAsync(client, $"{first}/accounts")).Count(a => a.GetProperty("isSystem").GetBoolean()));

        var code = UniqueCode("WSA");
        var accountInFirst = await CreateAccountAsync(client, first, code, "Solo en uno", "reserve");
        Assert.DoesNotContain(await GetArrayAsync(client, $"{second}/accounts"), a => a.GetProperty("id").GetString() == accountInFirst);

        // The same account code can be reused in another workspace.
        await CreateAccountAsync(client, second, code, "Solo en dos", "reserve");

        // A movement cannot be linked to an account of another workspace.
        var movement = await PostJsonAsync(client, $"{second}/bank-movements", new { date = "2026-08-18", description = "Mov", amount = -1m });
        using var crossLink = await client.PatchAsJsonAsync($"{second}/bank-movements/{movement.GetProperty("id").GetString()}",
            new { entityId = accountInFirst });
        Assert.Equal(HttpStatusCode.BadRequest, crossLink.StatusCode);
        Assert.Empty(await GetArrayAsync(client, $"{first}/bank-movements"));
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string userKey)
    {
        using var tokenClient = fixture.CreateOidcTokenClient();
        var accessToken = await tokenClient.GetAccessTokenAsync(userKey);

        var client = new HttpClient { BaseAddress = fixture.Client.BaseAddress };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static async Task<string> CreateWorkspaceAsync(HttpClient client, string name)
    {
        var workspace = await PostJsonAsync(client, "/server/workspaces", new { name });
        return $"/server/workspaces/{AssertNonEmptyString(workspace, "id")}";
    }

    private static async Task<string> CreateAccountAsync(HttpClient client, string ws, string code, string name, string type)
    {
        var account = await PostJsonAsync(client, $"{ws}/accounts", new
        {
            code,
            name,
            type,
            balance = 0m,
        });
        return AssertNonEmptyString(account, "id");
    }

    private static async Task<string> CreateJournalEntryAsync(HttpClient client, string ws, string bankAccountId, string expenseAccountId)
    {
        var entry = await PostJsonAsync(client, $"{ws}/journal-entries", new
        {
            date = "2026-08-18",
            description = "Asiento conciliacion acceptance",
            lines = new[]
            {
                new { accountId = expenseAccountId, debit = 42.50m, credit = 0m, description = "Gasto" },
                new { accountId = bankAccountId, debit = 0m, credit = 42.50m, description = "Banco" },
            },
        });
        return AssertNonEmptyString(entry, "id");
    }

    private static async Task<JsonElement> PostJsonAsync(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ParseJsonAsync(response);
    }

    private static async Task<JsonElement[]> GetArrayAsync(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ParseJsonAsync(response)).EnumerateArray().ToArray();
    }

    private static async Task<JsonElement> ParseJsonAsync(HttpResponseMessage response)
    {
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.Clone();
    }

    private static JsonElement AssertSingleById(JsonElement[] items, string id) =>
        Assert.Single(items, item => item.GetProperty("id").GetString() == id);

    private static string AssertNonEmptyString(JsonElement element, string propertyName)
    {
        var value = element.GetProperty(propertyName).GetString();
        Assert.False(string.IsNullOrWhiteSpace(value));
        return value;
    }

    private static string UniqueCode(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..12];
}
