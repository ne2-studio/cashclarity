using CashClarity.Api.Controllers;
using CashClarity.Api.Domain;
using CashClarity.Api.Repositories;
using Xunit;

namespace CashClarity.Api.Tests;

public class InMemoryRepositoriesTests
{
    [Fact]
    public async Task GetAccounts_creates_system_accounts_per_user()
    {
        var repo = new InMemoryAccountsRepository();

        var userA = await repo.GetAccounts("user-a");
        var userB = await repo.GetAccounts("user-b");

        Assert.Equal(["Cuenta Principal", "Sin categorizar"], userA.Select(a => a.Name).Order());
        Assert.All(userA, a => Assert.Equal("user-a", a.UserId));
        Assert.All(userB, a => Assert.Equal("user-b", a.UserId));
    }

    [Fact]
    public async Task Journal_entries_are_isolated_by_user()
    {
        var repo = new InMemoryJournalEntriesRepository();

        await repo.AddJournalEntry(new JournalEntryCreateRequest(
            "2026-08-16",
            "Ingreso",
            [
                new JournalLineRequest("account-a", 0m, 10m),
                new JournalLineRequest("account-b", 10m, 0m),
            ]), "user-a");

        Assert.Single(await repo.GetJournalEntries("user-a"));
        Assert.Empty(await repo.GetJournalEntries("user-b"));
    }

    [Fact]
    public async Task Adding_an_unbalanced_journal_entry_is_rejected()
    {
        var repo = new InMemoryJournalEntriesRepository();

        await Assert.ThrowsAsync<UnbalancedJournalEntryException>(() => repo.AddJournalEntry(
            new JournalEntryCreateRequest(
                "2026-08-16",
                "Ingreso",
                [new JournalLineRequest("account-a", 0m, 10m)]),
            "user-a"));

        Assert.Empty(await repo.GetJournalEntries("user-a"));
    }

    [Fact]
    public async Task Updating_a_journal_entry_to_be_unbalanced_is_rejected()
    {
        var repo = new InMemoryJournalEntriesRepository();
        var entry = await repo.AddJournalEntry(new JournalEntryCreateRequest(
            "2026-08-16",
            "Ingreso",
            [
                new JournalLineRequest("account-a", 0m, 10m),
                new JournalLineRequest("account-b", 10m, 0m),
            ]), "user-a");

        await Assert.ThrowsAsync<UnbalancedJournalEntryException>(() => repo.UpdateJournalEntry(
            entry.Id,
            new JournalEntryPatchRequest(Lines: [new JournalLineRequest("account-a", 0m, 5m)]),
            "user-a"));
    }

    [Fact]
    public async Task Deleting_an_account_still_referenced_by_journal_lines_is_rejected()
    {
        var journalEntries = new List<JournalEntryResponse>();
        var bankMovements = new List<BankMovementResponse>();
        var accountsRepo = new InMemoryAccountsRepository(journalEntries, bankMovements);
        var journalEntriesRepo = new InMemoryJournalEntriesRepository(journalEntries, bankMovements);

        var account = await accountsRepo.AddAccount(new AccountCreateRequest("5721", "Banco", "main"), "user-a");
        var otherAccount = await accountsRepo.AddAccount(new AccountCreateRequest("6000", "Gasto", "expense"), "user-a");
        await journalEntriesRepo.AddJournalEntry(new JournalEntryCreateRequest(
            "2026-08-16",
            "Movimiento",
            [
                new JournalLineRequest(account.Id, 5m, 0m),
                new JournalLineRequest(otherAccount.Id, 0m, 5m),
            ]), "user-a");

        await Assert.ThrowsAsync<AccountInUseException>(() => accountsRepo.DeleteAccount(account.Id, "user-a"));

        Assert.Contains(await accountsRepo.GetAccounts("user-a"), a => a.Id == account.Id);
    }
}
