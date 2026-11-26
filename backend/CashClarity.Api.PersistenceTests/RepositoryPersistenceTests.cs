using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;
using CashClarity.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace CashClarity.Api.PersistenceTests;

[Collection(PersistenceCollection.Name)]
public class RepositoryPersistenceTests(PostgresFixture fixture)
{
    [Fact]
    public async Task System_accounts_use_real_unique_constraint_without_duplicates()
    {
        await ResetDatabase();
        var ws = await CreateWorkspace("user-a");
        await using var db = fixture.CreateDbContext();
        var repo = new AccountsRepository(db);

        await repo.GetAccounts("user-a", ws);
        var accounts = await repo.GetAccounts("user-a", ws);

        Assert.Equal(2, accounts.Count(a => a.IsSystem));
        Assert.Equal(2, await db.Accounts.CountAsync(a => a.WorkspaceId == ws));
    }

    [Fact]
    public async Task Deleting_journal_entry_cascades_lines_in_postgres()
    {
        await ResetDatabase();
        var ws = await CreateWorkspace("user-a");
        await using var db = fixture.CreateDbContext();
        var accountsRepo = new AccountsRepository(db);
        var journalEntriesRepo = new JournalEntriesRepository(db);
        var account = await accountsRepo.AddAccount(new AccountCreateRequest("5721", "Banco", "main"), "user-a", ws);
        var otherAccount = await accountsRepo.AddAccount(new AccountCreateRequest("6000", "Gasto", "expense"), "user-a", ws);
        var entry = await journalEntriesRepo.AddJournalEntry(new JournalEntryCreateRequest(
            "2026-08-16",
            "Movimiento",
            [
                new JournalLineRequest(account.Id, 5m, 0m),
                new JournalLineRequest(otherAccount.Id, 0m, 5m),
            ]), "user-a", ws);

        await journalEntriesRepo.DeleteJournalEntry(entry.Id, "user-a", ws);

        Assert.Empty(await db.JournalLines.ToListAsync());
    }

    [Fact]
    public async Task Deleting_an_account_still_referenced_by_journal_lines_is_rejected_in_postgres()
    {
        await ResetDatabase();
        var ws = await CreateWorkspace("user-a");
        await using var db = fixture.CreateDbContext();
        var accountsRepo = new AccountsRepository(db);
        var journalEntriesRepo = new JournalEntriesRepository(db);
        var account = await accountsRepo.AddAccount(new AccountCreateRequest("5721", "Banco", "main"), "user-a", ws);
        var otherAccount = await accountsRepo.AddAccount(new AccountCreateRequest("6000", "Gasto", "expense"), "user-a", ws);
        await journalEntriesRepo.AddJournalEntry(new JournalEntryCreateRequest(
            "2026-08-16",
            "Movimiento",
            [
                new JournalLineRequest(account.Id, 5m, 0m),
                new JournalLineRequest(otherAccount.Id, 0m, 5m),
            ]), "user-a", ws);

        await Assert.ThrowsAsync<AccountInUseException>(() => accountsRepo.DeleteAccount(account.Id, "user-a", ws));

        Assert.Contains(await accountsRepo.GetAccounts("user-a", ws), a => a.Id == account.Id);
    }

    private async Task<Guid> CreateWorkspace(string userId)
    {
        await using var db = fixture.CreateDbContext();
        return Guid.Parse((await new WorkspacesRepository(db).CreateWorkspace("Principal", userId)).Id);
    }

    [Fact]
    public async Task Creating_a_workspace_seeds_system_accounts_and_isolates_data()
    {
        await ResetDatabase();
        var wsA = await CreateWorkspace("user-a");
        var wsB = await CreateWorkspace("user-a");
        var other = await CreateWorkspace("user-b");
        await using var db = fixture.CreateDbContext();
        var accounts = new AccountsRepository(db);
        var journal = new JournalEntriesRepository(db);
        var movements = new BankMovementsRepository(db);
        var workspaces = new WorkspacesRepository(db);

        Assert.Equal(2, await db.Accounts.CountAsync(a => a.WorkspaceId == wsA && a.IsSystem));
        Assert.Equal(2, (await workspaces.GetWorkspaces("user-a")).Count);
        Assert.True(await workspaces.UserOwnsWorkspace(wsA, "user-a"));
        Assert.False(await workspaces.UserOwnsWorkspace(wsA, "user-b"));

        // Same code in different workspaces is fine; a duplicate within a workspace is not.
        var bankA = await accounts.AddAccount(new AccountCreateRequest("5721", "Banco", "main"), "user-a", wsA);
        var gastoA = await accounts.AddAccount(new AccountCreateRequest("6000", "Gasto", "expense"), "user-a", wsA);
        await accounts.AddAccount(new AccountCreateRequest("5721", "Banco", "main"), "user-a", wsB);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            accounts.AddAccount(new AccountCreateRequest("5721", "Dup", "main"), "user-a", wsA));
        db.ChangeTracker.Clear();

        var lines = new[] { new JournalLineRequest(bankA.Id, 0m, 5m), new JournalLineRequest(gastoA.Id, 5m, 0m) };
        var entry = await journal.AddJournalEntry(new JournalEntryCreateRequest("2026-08-16", "Mov", lines.ToList()), "user-a", wsA);
        await movements.AddBankMovement(new BankMovementCreateRequest("2026-08-16", "Mov", -5m), "user-a", wsA);

        Assert.Empty(await journal.GetJournalEntries("user-a", wsB));
        Assert.Empty(await movements.GetBankMovements("user-a", wsB));
        Assert.DoesNotContain(await accounts.GetAccounts("user-a", wsB), a => a.Id == bankA.Id);
        Assert.Single(await journal.GetJournalEntries("user-a", wsA));

        // Cross-workspace references are rejected.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            journal.AddJournalEntry(new JournalEntryCreateRequest("2026-08-16", "Mix", lines.ToList()), "user-a", wsB));
        await Assert.ThrowsAsync<InvalidOperationException>(() => movements.AddBankMovement(
            new BankMovementCreateRequest("2026-08-16", "Mov", -5m, EntityId: gastoA.Id), "user-a", wsB));
        await Assert.ThrowsAsync<InvalidOperationException>(() => movements.AddBankMovement(
            new BankMovementCreateRequest("2026-08-16", "Mov", -5m, JournalEntryId: entry.Id), "user-a", wsB));
    }

    [Fact]
    public async Task Migration_backfills_a_default_workspace_per_existing_user()
    {
        // Build a database at the previous migration, insert legacy rows, then apply the new one.
        var connection = fixture.ConnectionString.Replace("Database=cashclarity_tests", "Database=cashclarity_backfill");
        await using (var admin = fixture.CreateDbContext())
        {
            await admin.Database.ExecuteSqlRawAsync("DROP DATABASE IF EXISTS cashclarity_backfill WITH (FORCE)");
            await admin.Database.ExecuteSqlRawAsync("CREATE DATABASE cashclarity_backfill");
        }
        var options = new DbContextOptionsBuilder<CashClarity.Api.Data.FinanceDbContext>().UseNpgsql(connection).Options;
        await using var db = new CashClarity.Api.Data.FinanceDbContext(options);
        var migrator = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        await migrator.MigrateAsync("RestrictAccountDeletionWithJournalLines");

        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO accounts (id, code, name, type, user_id) VALUES ('acc-1', '5721', 'Banco', 'main', 'legacy-a'), ('acc-2', '5721', 'Banco', 'main', 'legacy-b');
            INSERT INTO bank_movements (amount, description, user_id) VALUES (-1, 'Legacy', 'legacy-a');
            """);

        await migrator.MigrateAsync();

        var workspaces = await db.Workspaces.OrderBy(w => w.UserId).ToListAsync();
        Assert.Equal(["legacy-a", "legacy-b"], workspaces.Select(w => w.UserId));
        Assert.All(workspaces, w => Assert.Equal("Principal", w.Name));
        Assert.All(await db.Accounts.ToListAsync(), a =>
            Assert.Equal(workspaces.Single(w => w.UserId == a.UserId).Id, a.WorkspaceId));
        Assert.Equal(workspaces[0].Id, (await db.BankMovements.SingleAsync()).WorkspaceId);
    }

    [Fact]
    public async Task Synced_movements_are_inserted_once_per_external_id_and_workspace_in_postgres()
    {
        await ResetDatabase();
        var ws = await CreateWorkspace("user-a");
        var otherWs = await CreateWorkspace("user-a");
        await using var db = fixture.CreateDbContext();
        var repo = new BankMovementsRepository(db);
        List<SyncedBankMovementCreate> batch =
        [
            new("ref:1", new DateOnly(2026, 10, 20), "Nomina", 1500m),
            new("ref:2", new DateOnly(2026, 10, 21), "Supermercado", -42.30m),
        ];

        var first = await repo.AddSyncedBankMovements(batch, "user-a", ws);
        var second = await repo.AddSyncedBankMovements(batch, "user-a", ws);
        var otherWorkspace = await repo.AddSyncedBankMovements(batch, "user-a", otherWs);

        Assert.Equal(2, first.Count);
        Assert.Empty(second);
        Assert.Equal(2, otherWorkspace.Count);
        Assert.Equal(2, await db.BankMovements.CountAsync(bm => bm.WorkspaceId == ws));
        Assert.Equal(new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc), first[0].Date);
    }

    [Fact]
    public async Task Unique_index_rejects_a_second_movement_with_the_same_external_id()
    {
        await ResetDatabase();
        var ws = await CreateWorkspace("user-a");
        await using var db = fixture.CreateDbContext();
        db.BankMovements.Add(new BankMovement { Description = "a", Amount = 1m, UserId = "user-a", WorkspaceId = ws, ExternalId = "ref:1" });
        db.BankMovements.Add(new BankMovement { Description = "b", Amount = 1m, UserId = "user-a", WorkspaceId = ws, ExternalId = "ref:1" });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Bank_link_round_trips_dates_and_account_options_in_postgres()
    {
        await ResetDatabase();
        var ws = await CreateWorkspace("user-a");
        var link = new BankLink
        {
            Id = Guid.NewGuid(),
            WorkspaceId = ws,
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
            var stored = await repo.GetBankLink(ws);
            Assert.NotNull(stored);
            Assert.Equal(new DateOnly(2026, 10, 1), stored.SyncFrom);
            Assert.Equal(link.ValidUntil, stored.ValidUntil);
            Assert.Contains("uid-1", stored.AccountOptionsJson);

            stored.AccountUid = "uid-1";
            await repo.UpdateBankLink(stored);
            await Assert.ThrowsAsync<DbUpdateException>(() => repo.AddBankLink(new BankLink
            {
                Id = Guid.NewGuid(), WorkspaceId = ws, UserId = "user-a", AspspName = "N26", AspspCountry = "ES", PsuType = "personal",
            }));
        }

        await using (var db = fixture.CreateDbContext())
        {
            Assert.Equal("uid-1", (await new BankLinksRepository(db).GetBankLink(ws))!.AccountUid);
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
        await db.Workspaces.ExecuteDeleteAsync();
    }
}
