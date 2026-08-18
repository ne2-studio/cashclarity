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
            [new JournalLineRequest("account-a", 10m, 0m)]), "user-a");

        Assert.Single(await repo.GetJournalEntries("user-a"));
        Assert.Empty(await repo.GetJournalEntries("user-b"));
    }
}
