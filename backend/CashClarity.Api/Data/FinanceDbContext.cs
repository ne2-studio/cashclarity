using CashClarity.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Data;

public class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<BankMovement> BankMovements => Set<BankMovement>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
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
            e.Property(a => a.UserId).HasColumnName("user_id");
            e.Property(a => a.CreatedAt).HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
            e.Property(a => a.UpdatedAt).HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");

            e.HasIndex(a => new { a.UserId, a.Code }).IsUnique()
                .HasDatabaseName("accounts_user_id_code_key");
        });

        mb.Entity<JournalEntry>(e =>
        {
            e.ToTable("journal_entries");
            e.Property(je => je.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            e.Property(je => je.Date).HasColumnName("date")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
            e.Property(je => je.Description).HasColumnName("description");
            e.Property(je => je.UserId).HasColumnName("user_id");
            e.Property(je => je.CreatedAt).HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
            e.Property(je => je.UpdatedAt).HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
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
                .HasForeignKey(jl => jl.AccountId).OnDelete(DeleteBehavior.Cascade);

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
            e.Property(bm => bm.UserId).HasColumnName("user_id");
            e.Property(bm => bm.CreatedAt).HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");
            e.Property(bm => bm.UpdatedAt).HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("timezone('utc'::text, now())");

            e.HasOne<Account>().WithMany()
                .HasForeignKey(bm => bm.EntityId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            e.HasOne<JournalEntry>().WithMany()
                .HasForeignKey(bm => bm.JournalEntryId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(bm => bm.EntityId).HasDatabaseName("idx_bank_movements_entity_id");
            e.HasIndex(bm => bm.JournalEntryId).HasDatabaseName("idx_bank_movements_journal_entry_id");
        });
    }
}
