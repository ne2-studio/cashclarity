using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;
using CashClarity.Api.Repositories;
using CashClarity.Api.Services;
using CashClarity.Api.Services.EnableBanking;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CashClarity.Api.Tests.EnableBanking;

public class BankLinkServiceTests
{
    private const string User = "user-a";
    private readonly MockEnableBankingApi api = new();
    private readonly FixedTimeProvider clock = new(new DateTimeOffset(2026, 10, 24, 10, 0, 0, TimeSpan.Zero));
    private readonly List<BankMovementResponse> movementsStore = [];
    private readonly InMemoryBankLinksRepository links = new();
    private readonly BankLinkService service;

    public BankLinkServiceTests()
    {
        service = new BankLinkService(
            links,
            new InMemoryBankMovementsRepository(movementsStore),
            api.CreateClient(clock),
            clock,
            NullLogger<BankLinkService>.Instance);
    }

    [Fact]
    public async Task Linking_an_account_stores_session_account_and_consent_expiry()
    {
        var start = await service.StartAuthorization(new BankAuthorizationStartRequest("CaixaBank", "ES", "business", "2026-10-01"), User);

        var auth = api.Requests.Single(r => r.Uri.AbsolutePath == "/auth").JsonBody!;
        Assert.Equal("CaixaBank", auth["aspsp"]!["name"]!.GetValue<string>());
        Assert.Equal("ES", auth["aspsp"]!["country"]!.GetValue<string>());
        Assert.Equal("business", auth["psu_type"]!.GetValue<string>());
        Assert.Equal(MockEnableBankingApi.RedirectUrl, auth["redirect_url"]!.GetValue<string>());
        Assert.Equal(clock.Now.AddDays(90), DateTimeOffset.Parse(auth["access"]!["valid_until"]!.GetValue<string>()));
        var state = auth["state"]!.GetValue<string>();
        Assert.Equal($"https://bank.example/login?state={state}", start.Url);
        Assert.Equal(BankLinkStatus.Pending, (await service.GetBankLink(User)).Status);

        var link = await service.CompleteAuthorization(new BankAuthorizationCompleteRequest("code-1", state), User);

        Assert.Equal(BankLinkStatus.Active, link.Status);
        Assert.Equal("ES9121000418450200051332", link.Iban);
        Assert.Equal(api.SessionValidUntil.UtcDateTime, link.ValidUntil);
        Assert.Equal("2026-10-01", link.SyncFrom);
        Assert.Equal("{\"code\":\"code-1\"}", api.Requests.Single(r => r.Uri.AbsolutePath == "/sessions").Body);
    }

    [Fact]
    public async Task Consent_validity_is_capped_by_the_bank_maximum_and_psd2()
    {
        await service.StartAuthorization(new BankAuthorizationStartRequest("N26", "ES", "personal"), User);

        var auth = api.Requests.Single(r => r.Uri.AbsolutePath == "/auth").JsonBody!;
        Assert.Equal(clock.Now.AddDays(90), DateTimeOffset.Parse(auth["access"]!["valid_until"]!.GetValue<string>()));
    }

    [Fact]
    public async Task Completing_with_a_foreign_or_expired_state_is_rejected()
    {
        await service.StartAuthorization(new BankAuthorizationStartRequest("CaixaBank", "ES", "personal"), User);
        var state = StateSentToBank();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAuthorization(new BankAuthorizationCompleteRequest("code-1", "forged"), User));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAuthorization(new BankAuthorizationCompleteRequest("code-1", state), "user-b"));

        clock.Now = clock.Now.AddHours(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAuthorization(new BankAuthorizationCompleteRequest("code-1", state), User));
        Assert.DoesNotContain(api.Requests, r => r.Uri.AbsolutePath == "/sessions");
    }

    [Fact]
    public async Task Syncing_twice_in_a_row_does_not_duplicate_movements()
    {
        await LinkAccount(syncFrom: "2026-10-01");
        api.Transactions =
        [
            MockEnableBankingApi.Transaction("ref-1", "2026-10-20", "1500.00", "CRDT", "Nomina octubre"),
            MockEnableBankingApi.Transaction("ref-2", "2026-10-21", "42.30", "DBIT", "Supermercado"),
            MockEnableBankingApi.Transaction(null, "2026-10-22", "3.50", "DBIT", "Cafe"),
            MockEnableBankingApi.Transaction(null, "2026-10-22", "3.50", "DBIT", "Cafe"),
            MockEnableBankingApi.Transaction("ref-3", "2026-10-23", "10.00", "DBIT", "Pendiente", status: "PDNG"),
            MockEnableBankingApi.Transaction("ref-old", "2026-09-20", "99.00", "DBIT", "Antes de la fecha inicial"),
        ];

        var first = await service.Sync(User);
        var second = await service.Sync(User);

        Assert.Equal(4, first.Created.Count);
        Assert.Equal(0, first.SkippedExisting);
        Assert.Empty(second.Created);
        Assert.Equal(4, second.SkippedExisting);
        Assert.Equal(4, movementsStore.Count);
        Assert.All(movementsStore, m => Assert.False(m.IsIdentified));
        Assert.All(movementsStore, m => Assert.Null(m.EntityId));
        Assert.Contains(movementsStore, m => m.Description == "Nomina octubre" && m.Amount == 1500m && m.Date == new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc));
        Assert.Contains(movementsStore, m => m.Description == "Supermercado" && m.Amount == -42.30m);
        Assert.Equal(2, movementsStore.Count(m => m.Description == "Cafe"));
        Assert.Equal(1234.56m, first.Balance!.Amount);
    }

    [Fact]
    public async Task Sync_range_starts_at_sync_from_then_at_last_sync_minus_overlap()
    {
        await LinkAccount(syncFrom: "2026-09-01");

        var first = await service.Sync(User);
        clock.Now = clock.Now.AddDays(10);
        var second = await service.Sync(User);

        Assert.Equal(("2026-09-01", "2026-10-24"), (first.DateFrom, first.DateTo));
        Assert.Equal(("2026-10-17", "2026-11-03"), (second.DateFrom, second.DateTo));
        var lastQuery = api.Requests.Last(r => r.Uri.AbsolutePath.EndsWith("/transactions")).Uri.Query;
        Assert.Contains("date_from=2026-10-17", lastQuery);
        Assert.Contains("date_to=2026-11-03", lastQuery);
        Assert.Contains("transaction_status=BOOK", lastQuery);
    }

    [Fact]
    public async Task Sync_follows_continuation_keys_across_pages()
    {
        await LinkAccount(syncFrom: "2026-10-01");
        api.PageSize = 2;
        api.Transactions = Enumerable.Range(1, 5)
            .Select(i => MockEnableBankingApi.Transaction($"ref-{i}", $"2026-10-0{i}", "1.00", "DBIT", $"Movimiento {i}"))
            .ToList();

        var result = await service.Sync(User);

        Assert.Equal(5, result.Created.Count);
        Assert.Equal(3, api.Requests.Count(r => r.Uri.AbsolutePath.EndsWith("/transactions")));
    }

    [Fact]
    public async Task Sync_warns_about_movements_matching_existing_csv_imports()
    {
        movementsStore.Add(new BankMovementResponse("csv-1", new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc), "NOMINA OCT EMPRESA SL", 1500m, false, null, null, User));
        await LinkAccount(syncFrom: "2026-10-01");
        api.Transactions = [MockEnableBankingApi.Transaction("ref-1", "2026-10-02", "1500.00", "CRDT", "Nomina octubre")];

        var result = await service.Sync(User);

        var warning = Assert.Single(result.PossibleDuplicates);
        Assert.Equal("csv-1", warning.ExistingMovementId);
        Assert.Equal(result.Created.Single().Id, warning.MovementId);
    }

    [Fact]
    public async Task Default_sync_from_is_the_day_after_the_latest_existing_movement()
    {
        movementsStore.Add(new BankMovementResponse("csv-1", new DateTime(2026, 10, 10, 0, 0, 0, DateTimeKind.Utc), "Ultimo CSV", 1m, false, null, null, User));

        var link = await LinkAccount(syncFrom: null);

        Assert.Equal("2026-10-11", link.SyncFrom);
    }

    [Fact]
    public async Task Sync_with_expired_consent_is_rejected_and_link_reports_expired()
    {
        api.SessionValidUntil = clock.Now.AddDays(1);
        await LinkAccount(syncFrom: "2026-10-01");
        clock.Now = clock.Now.AddDays(2);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.Sync(User));

        Assert.Contains("caducado", error.Message);
        Assert.Equal(BankLinkStatus.Expired, (await service.GetBankLink(User)).Status);
    }

    [Fact]
    public async Task Sync_translates_revoked_session_from_the_bank_into_relink_message()
    {
        await LinkAccount(syncFrom: "2026-10-01");
        api.TransactionsError = "REVOKED_SESSION";

        var error = await Assert.ThrowsAsync<BankProviderException>(() => service.Sync(User));

        Assert.True(error.ConsentInvalid);
        Assert.Equal(400, ApiExceptionHandler.Map(error).Status);
    }

    [Fact]
    public async Task Relinking_keeps_history_and_matches_the_same_iban_with_a_new_uid()
    {
        await LinkAccount(syncFrom: "2026-10-01");
        api.Transactions = [MockEnableBankingApi.Transaction("ref-1", "2026-10-02", "1500.00", "CRDT", "Nomina octubre")];
        await service.Sync(User);

        api.Accounts = [MockEnableBankingApi.Account("uid-other", "ES7620770024003102575766"), MockEnableBankingApi.Account("uid-2", "ES9121000418450200051332")];
        api.SessionValidUntil = new DateTimeOffset(2027, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var relinked = await LinkAccount(syncFrom: null);
        var resync = await service.Sync(User);

        Assert.Equal(BankLinkStatus.Active, relinked.Status);
        Assert.Equal(new DateTime(2027, 4, 1, 0, 0, 0, DateTimeKind.Utc), relinked.ValidUntil);
        Assert.Contains(api.Requests, r => r.Method == HttpMethod.Get && r.Uri.AbsolutePath == "/accounts/uid-2/transactions");
        Assert.Contains(api.Requests, r => r.Method == HttpMethod.Delete && r.Uri.AbsolutePath == "/sessions/session-1");
        Assert.Empty(resync.Created);
        Assert.Single(movementsStore);
    }

    [Fact]
    public async Task Relinking_without_the_linked_iban_is_rejected_and_keeps_the_old_session()
    {
        await LinkAccount(syncFrom: "2026-10-01");
        api.Accounts = [MockEnableBankingApi.Account("uid-other", "ES7620770024003102575766")];

        await service.StartAuthorization(new BankAuthorizationStartRequest("CaixaBank", "ES", "business"), User);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteAuthorization(new BankAuthorizationCompleteRequest("code-2", StateSentToBank()), User));

        var link = await service.GetBankLink(User);
        Assert.Equal(BankLinkStatus.Active, link.Status);
        Assert.Equal("ES9121000418450200051332", link.Iban);
    }

    [Fact]
    public async Task Linking_a_different_bank_requires_unlinking_first()
    {
        await LinkAccount(syncFrom: "2026-10-01");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAuthorization(new BankAuthorizationStartRequest("N26", "ES", "personal"), User));
    }

    [Fact]
    public async Task Session_with_several_accounts_waits_for_the_user_to_choose_one()
    {
        api.Accounts = [MockEnableBankingApi.Account("uid-1", "ES9121000418450200051332"), MockEnableBankingApi.Account("uid-2", "ES7620770024003102575766")];

        var pending = await LinkAccount(syncFrom: "2026-10-01");
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.Sync(User));
        var active = await service.SelectAccount(new BankAccountSelectRequest("uid-2"), User);

        Assert.Equal(BankLinkStatus.AwaitingAccount, pending.Status);
        Assert.Equal(2, pending.AccountOptions!.Count);
        Assert.Equal(BankLinkStatus.Active, active.Status);
        Assert.Equal("ES7620770024003102575766", active.Iban);
        Assert.Null(active.AccountOptions);
    }

    [Fact]
    public async Task Unlinking_closes_the_session_and_keeps_movements()
    {
        await LinkAccount(syncFrom: "2026-10-01");
        api.Transactions = [MockEnableBankingApi.Transaction("ref-1", "2026-10-02", "1500.00", "CRDT", "Nomina octubre")];
        await service.Sync(User);

        await service.Unlink(User);

        Assert.Equal(BankLinkStatus.None, (await service.GetBankLink(User)).Status);
        Assert.Contains(api.Requests, r => r.Method == HttpMethod.Delete && r.Uri.AbsolutePath == "/sessions/session-1");
        Assert.Single(movementsStore);
    }

    [Fact]
    public async Task Unsupported_psu_type_is_rejected_before_calling_the_bank()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAuthorization(new BankAuthorizationStartRequest("N26", "ES", "business"), User));

        Assert.DoesNotContain(api.Requests, r => r.Uri.AbsolutePath == "/auth");
    }

    private async Task<BankLinkResponse> LinkAccount(string? syncFrom)
    {
        await service.StartAuthorization(new BankAuthorizationStartRequest("CaixaBank", "ES", "business", syncFrom), User);
        return await service.CompleteAuthorization(new BankAuthorizationCompleteRequest("code", StateSentToBank()), User);
    }

    private string StateSentToBank() =>
        api.Requests.Last(r => r.Uri.AbsolutePath == "/auth").JsonBody!["state"]!.GetValue<string>();
}
