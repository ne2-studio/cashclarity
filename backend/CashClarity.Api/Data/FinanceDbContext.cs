using CashClarity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Data;

public class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : DbContext(options)
{
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<BankMovement> BankMovements => Set<BankMovement>();
    public DbSet<BankLink> BankLinks => Set<BankLink>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Workspace>(e =>
        {
            e.ToTable("workspaces");
            e.Property(w => w.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(w => w.Name).HasColumnName("name").HasMaxLength(WorkspaceNamePolicy.MaxLength);
            e.Property(w => w.UserId).HasColumnName("user_id");
            e.Property(w => w.CreatedAt).HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
            e.Property(w => w.UpdatedAt).HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");

            e.HasIndex(w => w.UserId).HasDatabaseName("idx_workspaces_user_id");
        });

        mb.Entity<Account>(e =>
        {
            e.ToTable("accounts");
            e.Property(a => a.Id).HasColumnName("id").HasColumnType("text")
                .HasDefaultValueSql("gen_random_uuid()::text");
            e.Property(a => a.Code).HasColumnName("code");
            e.Property(a => a.Name).HasColumnName("name");
            e.Property(a => a.Type).HasColumnName("type");
            e.Property(a => a.Balance).HasColumnName("balance").HasPrecision(15, 2).HasDefaultValue(0m);
            e.Property(a => a.IsSystem).HasColumnName("is_system").HasDefaultValue(false);
            e.Property(a => a.WorkspaceId).HasColumnName("workspace_id");
            e.Property(a => a.UserId).HasColumnName("user_id");
            e.Property(a => a.CreatedAt).HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
            e.Property(a => a.UpdatedAt).HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");

            e.HasOne<Workspace>().WithMany()
                .HasForeignKey(a => a.WorkspaceId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(a => new { a.WorkspaceId, a.Code }).IsUnique()
                .HasDatabaseName("accounts_workspace_id_code_key");
        });

        mb.Entity<JournalEntry>(e =>
        {
            e.ToTable("journal_entries");
            e.Property(je => je.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(je => je.Date).HasColumnName("date")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
            e.Property(je => je.Description).HasColumnName("description");
            e.Property(je => je.WorkspaceId).HasColumnName("workspace_id");
            e.Property(je => je.UserId).HasColumnName("user_id");
            e.Property(je => je.CreatedAt).HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
            e.Property(je => je.UpdatedAt).HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");

            e.HasOne<Workspace>().WithMany()
                .HasForeignKey(je => je.WorkspaceId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(je => je.WorkspaceId).HasDatabaseName("idx_journal_entries_workspace_id");
        });

        mb.Entity<JournalLine>(e =>
        {
            e.ToTable("journal_lines");
            e.Property(jl => jl.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(jl => jl.JournalEntryId).HasColumnName("journal_entry_id");
            e.Property(jl => jl.AccountId).HasColumnName("account_id").HasColumnType("text");
            e.Property(jl => jl.Credit).HasColumnName("credit").HasPrecision(15, 2).HasDefaultValue(0m);
            e.Property(jl => jl.Debit).HasColumnName("debit").HasPrecision(15, 2).HasDefaultValue(0m);
            e.Property(jl => jl.Description).HasColumnName("description");
            e.Property(jl => jl.CreatedAt).HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
            e.Property(jl => jl.UpdatedAt).HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");

            e.HasOne<JournalEntry>().WithMany(je => je.Lines)
                .HasForeignKey(jl => jl.JournalEntryId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Account>().WithMany()
                .HasForeignKey(jl => jl.AccountId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(jl => jl.JournalEntryId).HasDatabaseName("idx_journal_lines_journal_entry_id");
            e.HasIndex(jl => jl.AccountId).HasDatabaseName("idx_journal_lines_account_id");
        });

        mb.Entity<BankMovement>(e =>
        {
            e.ToTable("bank_movements");
            e.Property(bm => bm.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(bm => bm.Date).HasColumnName("date")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
            e.Property(bm => bm.Description).HasColumnName("description");
            e.Property(bm => bm.Amount).HasColumnName("amount").HasPrecision(15, 2);
            e.Property(bm => bm.IsIdentified).HasColumnName("is_identified").HasDefaultValue(false);
            e.Property(bm => bm.EntityId).HasColumnName("entity_id").HasColumnType("text");
            e.Property(bm => bm.JournalEntryId).HasColumnName("journal_entry_id");
            e.Property(bm => bm.WorkspaceId).HasColumnName("workspace_id");
            e.Property(bm => bm.UserId).HasColumnName("user_id");
            e.Property(bm => bm.ExternalId).HasColumnName("external_id");
            e.Property(bm => bm.CreatedAt).HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
            e.Property(bm => bm.UpdatedAt).HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");

            e.HasOne<Workspace>().WithMany()
                .HasForeignKey(bm => bm.WorkspaceId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Account>().WithMany()
                .HasForeignKey(bm => bm.EntityId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<JournalEntry>().WithMany()
                .HasForeignKey(bm => bm.JournalEntryId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(bm => bm.WorkspaceId).HasDatabaseName("idx_bank_movements_workspace_id");
            e.HasIndex(bm => bm.EntityId).HasDatabaseName("idx_bank_movements_entity_id");
            e.HasIndex(bm => bm.JournalEntryId).HasDatabaseName("idx_bank_movements_journal_entry_id");
            e.HasIndex(bm => new { bm.WorkspaceId, bm.ExternalId }).IsUnique()
                .HasFilter("external_id IS NOT NULL")
                .HasDatabaseName("bank_movements_workspace_id_external_id_key");
        });

        mb.Entity<BankLink>(e =>
        {
            e.ToTable("bank_links");
            e.Property(bl => bl.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(bl => bl.WorkspaceId).HasColumnName("workspace_id");
            e.Property(bl => bl.UserId).HasColumnName("user_id");
            e.Property(bl => bl.AspspName).HasColumnName("aspsp_name");
            e.Property(bl => bl.AspspCountry).HasColumnName("aspsp_country");
            e.Property(bl => bl.PsuType).HasColumnName("psu_type");
            e.Property(bl => bl.SyncFrom).HasColumnName("sync_from");
            e.Property(bl => bl.PendingState).HasColumnName("pending_state");
            e.Property(bl => bl.PendingStateExpiresAt).HasColumnName("pending_state_expires_at")
                .HasColumnType("timestamp with time zone");
            e.Property(bl => bl.SessionId).HasColumnName("session_id");
            e.Property(bl => bl.ValidUntil).HasColumnName("valid_until")
                .HasColumnType("timestamp with time zone");
            e.Property(bl => bl.AccountOptionsJson).HasColumnName("account_options").HasColumnType("jsonb");
            e.Property(bl => bl.AccountUid).HasColumnName("account_uid");
            e.Property(bl => bl.Iban).HasColumnName("iban");
            e.Property(bl => bl.AccountName).HasColumnName("account_name");
            e.Property(bl => bl.Currency).HasColumnName("currency");
            e.Property(bl => bl.LastSyncedAt).HasColumnName("last_synced_at")
                .HasColumnType("timestamp with time zone");
            e.Property(bl => bl.CreatedAt).HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
            e.Property(bl => bl.UpdatedAt).HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");

            e.HasOne<Workspace>().WithMany()
                .HasForeignKey(bl => bl.WorkspaceId).OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(bl => bl.WorkspaceId).IsUnique().HasDatabaseName("bank_links_workspace_id_key");
        });
    }
}
