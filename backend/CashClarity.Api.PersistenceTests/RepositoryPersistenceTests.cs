using CashClarity.Api.Contracts;
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
        var otherAccount = await accountsRepo.AddAccount(new AccountCreateRequest("6000", "Gasto", "expense"), "user-a");
        var entry = await journalEntriesRepo.AddJournalEntry(new JournalEntryCreateRequest(
            "2026-08-16",
            "Movimiento",
            [
                new JournalLineRequest(account.Id, 5m, 0m),
                new JournalLineRequest(otherAccount.Id, 0m, 5m),
            ]), "user-a");

        await journalEntriesRepo.DeleteJournalEntry(entry.Id, "user-a");

        Assert.Empty(await db.JournalLines.ToListAsync());
    }

    [Fact]
    public async Task Deleting_an_account_still_referenced_by_journal_lines_is_rejected_in_postgres()
    {
        await ResetDatabase();
        await using var db = fixture.CreateDbContext();
        var accountsRepo = new AccountsRepository(db);
        var journalEntriesRepo = new JournalEntriesRepository(db);
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

    [Fact]
    public async Task Synced_movements_are_inserted_once_per_external_id_in_postgres()
    {
        await ResetDatabase();
        await using var db = fixture.CreateDbContext();
        var repo = new BankMovementsRepository(db);
        List<SyncedBankMovementCreate> batch =
        [
            new("ref:1", new DateOnly(2026, 10, 20), "Nomina", 1500m),
            new("ref:2", new DateOnly(2026, 10, 21), "Supermercado", -42.30m),
        ];

        var first = await repo.AddSyncedBankMovements(batch, "user-a");
        var second = await repo.AddSyncedBankMovements(batch, "user-a");
        var otherUser = await repo.AddSyncedBankMovements(batch, "user-b");

        Assert.Equal(2, first.Count);
        Assert.Empty(second);
        Assert.Equal(2, otherUser.Count);
        Assert.Equal(2, await db.BankMovements.CountAsync(bm => bm.UserId == "user-a"));
        Assert.Equal(new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc), first[0].Date);
    }

    [Fact]
    public async Task Unique_index_rejects_a_second_movement_with_the_same_external_id()
    {
        await ResetDatabase();
        await using var db = fixture.CreateDbContext();
        db.BankMovements.Add(new BankMovement { Description = "a", Amount = 1m, UserId = "user-a", ExternalId = "ref:1" });
        db.BankMovements.Add(new BankMovement { Description = "b", Amount = 1m, UserId = "user-a", ExternalId = "ref:1" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Bank_link_round_trips_dates_and_account_options_in_postgres()
    {
        await ResetDatabase();
        var link = new BankLink
        {
            Id = Guid.NewGuid(),
            UserId = "user-a",
            AspspName = "CaixaBank",
            AspspCountry = "ES",
            PsuType = "business",
            SyncFrom = new DateOnly(2026, 10, 1),
            SessionId = "session-1",
            ValidUntil = new DateTime(2027, 1, 22, 10, 0, 0, DateTimeKind.Utc),
            AccountOptionsJson = "[{\"Uid\":\"uid-1\",\"Iban\":\"ES91\",\"Name\":null,\"Currency\":\"EUR\"}]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        await using (var db = fixture.CreateDbContext())
        {
            await new BankLinksRepository(db).AddBankLink(link);
        }

        await using (var db = fixture.CreateDbContext())
        {
            var repo = new BankLinksRepository(db);
            var stored = await repo.GetBankLink("user-a");
            Assert.NotNull(stored);
            Assert.Equal(new DateOnly(2026, 10, 1), stored.SyncFrom);
            Assert.Equal(link.ValidUntil, stored.ValidUntil);
            Assert.Contains("uid-1", stored.AccountOptionsJson);

            stored.AccountUid = "uid-1";
            await repo.UpdateBankLink(stored);
            await Assert.ThrowsAsync<DbUpdateException>(() => repo.AddBankLink(new BankLink
            {
                Id = Guid.NewGuid(), UserId = "user-a", AspspName = "N26", AspspCountry = "ES", PsuType = "personal",
            }));
        }

        await using (var db = fixture.CreateDbContext())
        {
            Assert.Equal("uid-1", (await new BankLinksRepository(db).GetBankLink("user-a"))!.AccountUid);
        }
    }

    private async Task ResetDatabase()
    {
        await using var db = fixture.CreateDbContext();
        await db.BankLinks.ExecuteDeleteAsync();
        await db.BankMovements.ExecuteDeleteAsync();
        await db.JournalLines.ExecuteDeleteAsync();
        await db.JournalEntries.ExecuteDeleteAsync();
        await db.Accounts.ExecuteDeleteAsync();
    }
}
