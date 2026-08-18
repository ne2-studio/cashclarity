using CashClarity.Api.Domain;
using CashClarity.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CashClarity.Api.PersistenceTests;

[Collection(PersistenceCollection.Name)]
public class RepositoryPersistenceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task System_accounts_use_real_unique_constraint_without_duplicates()
    {
        await ResetDatabase();
        await using var db = fixture.CreateDbContext();
        var repo = new AccountsRepository(db);

        await repo.GetAccounts("user-a");
        var accounts = await repo.GetAccounts("user-a");

        Assert.Equal(2, accounts.Count(a => a.IsSystem));
        Assert.Equal(2, await db.Accounts.CountAsync(a => a.UserId == "user-a"));
    }

    [Fact]
    public async Task Deleting_journal_entry_cascades_lines_in_postgres()
    {
        await ResetDatabase();
        await using var db = fixture.CreateDbContext();
        var accountsRepo = new AccountsRepository(db);
        var journalEntriesRepo = new JournalEntriesRepository(db);
        var account = await accountsRepo.AddAccount(new AccountCreateRequest("5721", "Banco", "main"), "user-a");
        var entry = await journalEntriesRepo.AddJournalEntry(new JournalEntryCreateRequest(
            "2026-08-16",
            "Movimiento",
            [new JournalLineRequest(account.Id, 5m, 0m)]), "user-a");

        await journalEntriesRepo.DeleteJournalEntry(entry.Id, "user-a");

        Assert.Empty(await db.JournalLines.ToListAsync());
    }

    private async Task ResetDatabase()
    {
        await using var db = fixture.CreateDbContext();
        await db.BankMovements.ExecuteDeleteAsync();
        await db.JournalLines.ExecuteDeleteAsync();
        await db.JournalEntries.ExecuteDeleteAsync();
        await db.Accounts.ExecuteDeleteAsync();
    }
}
