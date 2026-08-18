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

        var initialAccounts = await GetArrayAsync(client, "/server/accounts");
        Assert.Contains(initialAccounts, account =>
            account.GetProperty("code").GetString() == "0001" &&
            account.GetProperty("isSystem").GetBoolean());
        Assert.Contains(initialAccounts, account =>
            account.GetProperty("code").GetString() == "9999" &&
            account.GetProperty("isSystem").GetBoolean());

        var code = UniqueCode("RES");
        var account = await PostJsonAsync(client, "/server/accounts", new
        {
            code,
            name = "Reserva IVA acceptance",
            type = "reserve",
            balance = 123.45m,
        });
        var accountId = AssertNonEmptyString(account, "id");
        Assert.Equal(code, account.GetProperty("code").GetString());
        Assert.Equal("acceptance-user", account.GetProperty("userId").GetString());

        var patchResponse = await client.PatchAsJsonAsync($"/server/accounts/{accountId}", new
        {
            name = "Reserva impuestos acceptance",
            balance = 200m,
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var updatedAccounts = await GetArrayAsync(client, "/server/accounts");
        var updated = AssertSingleById(updatedAccounts, accountId);
        Assert.Equal("Reserva impuestos acceptance", updated.GetProperty("name").GetString());
        Assert.Equal(200m, updated.GetProperty("balance").GetDecimal());

        var deleteResponse = await client.DeleteAsync($"/server/accounts/{accountId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var finalAccounts = await GetArrayAsync(client, "/server/accounts");
        Assert.DoesNotContain(finalAccounts, item => item.GetProperty("id").GetString() == accountId);
    }

    [Fact]
    public async Task Manual_accounting_journey_records_updates_and_removes_a_journal_entry()
    {
        using var client = await CreateAuthenticatedClientAsync(AcceptanceFixture.OidcUserKey);
        var bankAccountId = await CreateAccountAsync(client, UniqueCode("BNK"), "Banco acceptance", "main");
        var reserveAccountId = await CreateAccountAsync(client, UniqueCode("TAX"), "Impuestos acceptance", "reserve");

        var entry = await PostJsonAsync(client, "/server/journal-entries", new
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

        var listedEntry = AssertSingleById(await GetArrayAsync(client, "/server/journal-entries"), entryId);
        Assert.Equal("Provision impuestos acceptance", listedEntry.GetProperty("description").GetString());

        var patchResponse = await client.PatchAsJsonAsync($"/server/journal-entries/{entryId}", new
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

        var updatedEntry = AssertSingleById(await GetArrayAsync(client, "/server/journal-entries"), entryId);
        Assert.Equal("Provision impuestos ajustada", updatedEntry.GetProperty("description").GetString());
        Assert.Contains(updatedEntry.GetProperty("lines").EnumerateArray(), line => line.GetProperty("debit").GetDecimal() == 250m);

        var deleteResponse = await client.DeleteAsync($"/server/journal-entries/{entryId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        Assert.DoesNotContain(await GetArrayAsync(client, "/server/journal-entries"), item => item.GetProperty("id").GetString() == entryId);
    }

    [Fact]
    public async Task Bank_movement_reconciliation_journey_imports_identifies_links_and_removes_a_movement()
    {
        using var client = await CreateAuthenticatedClientAsync(AcceptanceFixture.OidcUserKey);
        var bankAccountId = await CreateAccountAsync(client, UniqueCode("BNK"), "Banco conciliacion acceptance", "main");
        var expenseAccountId = await CreateAccountAsync(client, UniqueCode("EXP"), "Gasto acceptance", "expense");
        var entryId = await CreateJournalEntryAsync(client, bankAccountId, expenseAccountId);

        var movement = await PostJsonAsync(client, "/server/bank-movements", new
        {
            date = "2026-08-18",
            description = "Cargo tarjeta pendiente",
            amount = -42.50m,
        });
        var movementId = AssertNonEmptyString(movement, "id");
        Assert.False(movement.GetProperty("isIdentified").GetBoolean());

        var patchResponse = await client.PatchAsJsonAsync($"/server/bank-movements/{movementId}", new
        {
            description = "Cargo tarjeta conciliado",
            isIdentified = true,
            entityId = expenseAccountId,
            journalEntryId = entryId,
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var reconciled = AssertSingleById(await GetArrayAsync(client, "/server/bank-movements"), movementId);
        Assert.True(reconciled.GetProperty("isIdentified").GetBoolean());
        Assert.Equal(expenseAccountId, reconciled.GetProperty("entityId").GetString());
        Assert.Equal(entryId, reconciled.GetProperty("journalEntryId").GetString());

        var deleteResponse = await client.DeleteAsync($"/server/bank-movements/{movementId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        Assert.DoesNotContain(await GetArrayAsync(client, "/server/bank-movements"), item => item.GetProperty("id").GetString() == movementId);
    }

    [Fact]
    public async Task User_data_is_isolated_between_authenticated_callers()
    {
        using var primaryClient = await CreateAuthenticatedClientAsync(AcceptanceFixture.OidcUserKey);
        using var secondClient = await CreateAuthenticatedClientAsync(AcceptanceFixture.OidcSecondUserKey);

        var code = UniqueCode("ISO");
        var accountId = await CreateAccountAsync(primaryClient, code, "Cuenta privada acceptance", "reserve");

        var primaryAccounts = await GetArrayAsync(primaryClient, "/server/accounts");
        Assert.Contains(primaryAccounts, account => account.GetProperty("id").GetString() == accountId);

        var secondAccounts = await GetArrayAsync(secondClient, "/server/accounts");
        Assert.DoesNotContain(secondAccounts, account => account.GetProperty("code").GetString() == code);
        Assert.All(secondAccounts, account => Assert.Equal("second-acceptance-user", account.GetProperty("userId").GetString()));
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string userKey)
    {
        using var tokenClient = fixture.CreateOidcTokenClient();
        var accessToken = await tokenClient.GetAccessTokenAsync(userKey);

        var client = new HttpClient { BaseAddress = fixture.Client.BaseAddress };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static async Task<string> CreateAccountAsync(HttpClient client, string code, string name, string type)
    {
        var account = await PostJsonAsync(client, "/server/accounts", new
        {
            code,
            name,
            type,
            balance = 0m,
        });
        return AssertNonEmptyString(account, "id");
    }

    private static async Task<string> CreateJournalEntryAsync(HttpClient client, string bankAccountId, string expenseAccountId)
    {
        var entry = await PostJsonAsync(client, "/server/journal-entries", new
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
