using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;
using CashClarity.Api.Repositories;
using Xunit;

namespace CashClarity.Api.Tests;

public class InMemoryRepositoriesTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid WsB = Guid.NewGuid();

    private sealed class Store
    {
        public readonly List<AccountResponse> Accounts = [];
        public readonly List<JournalEntryResponse> Entries = [];
        public readonly List<BankMovementResponse> Movements = [];
        public InMemoryAccountsRepository AccountsRepo => new(Accounts, Entries, Movements);
        public InMemoryJournalEntriesRepository JournalRepo => new(Accounts, Entries, Movements);
        public InMemoryBankMovementsRepository MovementsRepo => new(Movements, Accounts, Entries);
    }

    private static async Task<(string Bank, string Other)> TwoAccounts(Store store, Guid ws)
    {
        var bank = await store.AccountsRepo.AddAccount(new AccountCreateRequest("5721", "Banco", "main"), "user-a", ws);
        var other = await store.AccountsRepo.AddAccount(new AccountCreateRequest("6000", "Gasto", "expense"), "user-a", ws);
        return (bank.Id, other.Id);
    }

    private static JournalEntryCreateRequest Entry(string a, string b) => new(
        "2026-08-16",
        "Ingreso",
        [new JournalLineRequest(a, 0m, 10m), new JournalLineRequest(b, 10m, 0m)]);

    [Fact]
    public async Task GetAccounts_creates_system_accounts_per_workspace()
    {
        var repo = new InMemoryAccountsRepository();

        var wsA = await repo.GetAccounts("user-a", WsA);
        var wsB = await repo.GetAccounts("user-a", WsB);

        Assert.Equal(["Cuenta Principal", "Sin categorizar"], wsA.Select(a => a.Name).Order());
        Assert.All(wsA, a => Assert.Equal(WsA.ToString(), a.WorkspaceId));
        Assert.All(wsB, a => Assert.Equal(WsB.ToString(), a.WorkspaceId));
        Assert.Empty(wsA.Select(a => a.Id).Intersect(wsB.Select(a => a.Id)));
    }

    [Fact]
    public async Task Same_account_code_is_allowed_in_different_workspaces_but_not_within_one()
    {
        var repo = new InMemoryAccountsRepository();
        await repo.AddAccount(new AccountCreateRequest("5721", "Banco", "main"), "user-a", WsA);
        await repo.AddAccount(new AccountCreateRequest("5721", "Banco", "main"), "user-a", WsB);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.AddAccount(new AccountCreateRequest("5721", "Otra", "main"), "user-a", WsA));
    }

    [Fact]
    public async Task Journal_entries_are_isolated_by_workspace()
    {
        var store = new Store();
        var (bank, other) = await TwoAccounts(store, WsA);

        await store.JournalRepo.AddJournalEntry(Entry(bank, other), "user-a", WsA);

        Assert.Single(await store.JournalRepo.GetJournalEntries("user-a", WsA));
        Assert.Empty(await store.JournalRepo.GetJournalEntries("user-a", WsB));
    }

    [Fact]
    public async Task Journal_lines_cannot_reference_accounts_of_another_workspace()
    {
        var store = new Store();
        var (bank, other) = await TwoAccounts(store, WsA);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.JournalRepo.AddJournalEntry(Entry(bank, other), "user-a", WsB));
    }

    [Fact]
    public async Task Bank_movements_are_isolated_and_links_stay_within_the_workspace()
    {
        var store = new Store();
        var (_, other) = await TwoAccounts(store, WsA);

        await store.MovementsRepo.AddBankMovement(new BankMovementCreateRequest("2026-08-16", "Mov", -5m), "user-a", WsA);

        Assert.Single(await store.MovementsRepo.GetBankMovements("user-a", WsA));
        Assert.Empty(await store.MovementsRepo.GetBankMovements("user-a", WsB));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.MovementsRepo.AddBankMovement(
            new BankMovementCreateRequest("2026-08-16", "Mov", -5m, EntityId: other), "user-a", WsB));
    }

    [Fact]
    public async Task Workspaces_are_listed_and_owned_per_user()
    {
        var repo = new InMemoryWorkspacesRepository();
        var created = await repo.CreateWorkspace("  Casa  ", "user-a");

        Assert.Equal("Casa", created.Name);
        Assert.Single(await repo.GetWorkspaces("user-a"));
        Assert.Empty(await repo.GetWorkspaces("user-b"));
        Assert.True(await repo.UserOwnsWorkspace(Guid.Parse(created.Id), "user-a"));
        Assert.False(await repo.UserOwnsWorkspace(Guid.Parse(created.Id), "user-b"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Creating_a_workspace_with_an_empty_name_is_rejected(string? name)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new InMemoryWorkspacesRepository().CreateWorkspace(name, "user-a"));
    }

    [Fact]
    public async Task Creating_a_workspace_with_a_too_long_name_is_rejected()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new InMemoryWorkspacesRepository().CreateWorkspace(new string('x', 101), "user-a"));
    }

    [Fact]
    public async Task Adding_an_unbalanced_journal_entry_is_rejected()
    {
        var store = new Store();
        var (bank, _) = await TwoAccounts(store, WsA);

        await Assert.ThrowsAsync<UnbalancedJournalEntryException>(() => store.JournalRepo.AddJournalEntry(
            new JournalEntryCreateRequest("2026-08-16", "Ingreso", [new JournalLineRequest(bank, 0m, 10m)]),
            "user-a", WsA));

        Assert.Empty(await store.JournalRepo.GetJournalEntries("user-a", WsA));
    }

    [Fact]
    public async Task Updating_a_journal_entry_to_be_unbalanced_is_rejected()
    {
        var store = new Store();
        var (bank, other) = await TwoAccounts(store, WsA);
        var entry = await store.JournalRepo.AddJournalEntry(Entry(bank, other), "user-a", WsA);

        await Assert.ThrowsAsync<UnbalancedJournalEntryException>(() => store.JournalRepo.UpdateJournalEntry(
            entry.Id,
            new JournalEntryPatchRequest(Lines: [new JournalLineRequest(bank, 0m, 5m)]),
            "user-a", WsA));
    }

    [Fact]
    public async Task Deleting_an_account_still_referenced_by_journal_lines_is_rejected()
    {
        var store = new Store();
        var (bank, other) = await TwoAccounts(store, WsA);
        await store.JournalRepo.AddJournalEntry(Entry(bank, other), "user-a", WsA);

        await Assert.ThrowsAsync<AccountInUseException>(() => store.AccountsRepo.DeleteAccount(bank, "user-a", WsA));

        Assert.Contains(await store.AccountsRepo.GetAccounts("user-a", WsA), a => a.Id == bank);
    }
}
