using CashClarity.Api.Data;
using CashClarity.Api.Domain;
using CashClarity.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Repositories;

public class FinanceRepository(FinanceDbContext db)
{
    private static readonly (string Code, string Name, string Type)[] SystemAccounts =
    [
        ("0001", "Cuenta Principal", "main"),
        ("9999", "Sin categorizar", "uncategorized"),
    ];

    // Accounts

    public async Task<List<AccountResponse>> GetAccounts(string userId)
    {
        await EnsureSystemAccounts(userId);
        return await db.Accounts
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .Select(a => new AccountResponse(a.Id, a.Code, a.Name, a.Type, a.Balance, a.IsSystem, userId))
            .ToListAsync();
    }

    private async Task EnsureSystemAccounts(string userId)
    {
        foreach (var (code, name, type) in SystemAccounts)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO accounts (code, name, type, is_system, balance, user_id)
                VALUES ({code}, {name}, {type}, true, 0, {userId})
                ON CONFLICT (user_id, code) DO NOTHING
                """);
        }
    }

    public async Task<AccountResponse> AddAccount(AccountCreateRequest req, string userId)
    {
        var account = new Account
        {
            Code = req.Code,
            Name = req.Name,
            Type = req.Type,
            Balance = req.Balance,
            IsSystem = req.IsSystem ?? false,
            UserId = userId,
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return MapAccount(account, userId);
    }

    public async Task UpdateAccount(string id, AccountPatchRequest patch, string userId)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId)
            ?? throw new Exception("Account not found or access denied");

        if (patch.Code is not null) account.Code = patch.Code;
        if (patch.Name is not null) account.Name = patch.Name;
        if (patch.Type is not null) account.Type = patch.Type;
        if (patch.Balance.HasValue) account.Balance = patch.Balance.Value;
        if (patch.IsSystem.HasValue) account.IsSystem = patch.IsSystem.Value;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAccount(string id, string userId)
    {
        await db.Accounts
            .Where(a => a.Id == id && a.UserId == userId)
            .ExecuteDeleteAsync();
    }

    // Journal Entries

    public async Task<List<JournalEntryResponse>> GetJournalEntries(string userId)
    {
        var entries = await db.JournalEntries
            .Include(je => je.Lines)
            .Where(je => je.UserId == userId)
            .OrderByDescending(je => je.Date)
            .ToListAsync();

        return entries.Select(e => MapEntry(e, userId)).ToList();
    }

    public async Task<JournalEntryResponse> AddJournalEntry(JournalEntryCreateRequest req, string userId)
    {
        var entry = new JournalEntry
        {
            Date = ParseDate(req.Date),
            Description = req.Description,
            UserId = userId,
            Lines = req.Lines.Select(l => new JournalLine
            {
                AccountId = l.AccountId,
                Credit = l.Credit,
                Debit = l.Debit,
                Description = l.Description,
            }).ToList(),
        };
        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync();
        return MapEntry(entry, userId);
    }

    public async Task UpdateJournalEntry(string id, JournalEntryPatchRequest patch, string userId)
    {
        var entryId = Guid.Parse(id);
        var entry = await db.JournalEntries
            .Include(je => je.Lines)
            .FirstOrDefaultAsync(je => je.Id == entryId && je.UserId == userId)
            ?? throw new Exception("Journal entry not found or access denied");

        if (patch.Date is not null) entry.Date = ParseDate(patch.Date);
        if (patch.Description is not null) entry.Description = patch.Description;

        if (patch.Lines is not null)
        {
            entry.Lines.Clear();
            foreach (var l in patch.Lines)
            {
                entry.Lines.Add(new JournalLine
                {
                    AccountId = l.AccountId,
                    Credit = l.Credit,
                    Debit = l.Debit,
                    Description = l.Description,
                });
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteJournalEntry(string id, string userId)
    {
        await db.JournalEntries
            .Where(je => je.Id == Guid.Parse(id) && je.UserId == userId)
            .ExecuteDeleteAsync();
    }

    // Bank Movements

    public async Task<List<BankMovementResponse>> GetBankMovements(string userId)
    {
        var movements = await db.BankMovements
            .Where(bm => bm.UserId == userId)
            .OrderByDescending(bm => bm.Date)
            .ToListAsync();

        return movements.Select(MapBankMovement).ToList();
    }

    public async Task<BankMovementResponse> AddBankMovement(BankMovementCreateRequest req, string userId)
    {
        var movement = new BankMovement
        {
            Date = ParseDate(req.Date),
            Description = req.Description,
            Amount = req.Amount,
            EntityId = req.EntityId,
            JournalEntryId = req.JournalEntryId is null ? null : Guid.Parse(req.JournalEntryId),
            IsIdentified = false,
            UserId = userId,
        };
        db.BankMovements.Add(movement);
        await db.SaveChangesAsync();
        return MapBankMovement(movement);
    }

    public async Task UpdateBankMovement(string id, BankMovementPatchRequest patch, string userId)
    {
        var movement = await db.BankMovements
            .FirstOrDefaultAsync(bm => bm.Id == Guid.Parse(id) && bm.UserId == userId)
            ?? throw new Exception("Bank movement not found or access denied");

        if (patch.Date is not null) movement.Date = ParseDate(patch.Date);
        if (patch.Description is not null) movement.Description = patch.Description;
        if (patch.Amount.HasValue) movement.Amount = patch.Amount.Value;
        if (patch.IsIdentified.HasValue) movement.IsIdentified = patch.IsIdentified.Value;
        if (patch.EntityId is not null) movement.EntityId = patch.EntityId;
        if (patch.JournalEntryId is not null) movement.JournalEntryId = Guid.Parse(patch.JournalEntryId);

        await db.SaveChangesAsync();
    }

    public async Task DeleteBankMovement(string id, string userId)
    {
        await db.BankMovements
            .Where(bm => bm.Id == Guid.Parse(id) && bm.UserId == userId)
            .ExecuteDeleteAsync();
    }

    // Mappers

    private static AccountResponse MapAccount(Account a, string userId) =>
        new(a.Id, a.Code, a.Name, a.Type, a.Balance, a.IsSystem, userId);

    private static JournalLineResponse MapLine(JournalLine l) =>
        new(l.Id.ToString(), l.AccountId, l.Credit, l.Debit, l.Description);

    private static JournalEntryResponse MapEntry(JournalEntry e, string userId) =>
        new(e.Id.ToString(), e.Date, e.Description, e.Lines.Select(MapLine).ToList(), userId);

    private static BankMovementResponse MapBankMovement(BankMovement bm) =>
        new(bm.Id.ToString(), bm.Date, bm.Description, bm.Amount, bm.IsIdentified,
            bm.EntityId, bm.JournalEntryId?.ToString(), bm.UserId.ToString());

    private static DateTime ParseDate(string date) =>
        DateTime.Parse(date, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal);
}
