using CashClarity.Api.Domain;

namespace CashClarity.Api.Repositories;

public class InMemoryFinanceRepository : IFinanceRepository
{
    private static readonly (string Code, string Name, string Type)[] SystemAccounts =
    [
        ("0001", "Cuenta Principal", "main"),
        ("9999", "Sin categorizar", "uncategorized"),
    ];

    private readonly object gate = new();
    private readonly List<AccountResponse> accounts = [];
    private readonly List<JournalEntryResponse> journalEntries = [];
    private readonly List<BankMovementResponse> bankMovements = [];

    public Task<List<AccountResponse>> GetAccounts(string userId)
    {
        lock (gate)
        {
            EnsureSystemAccounts(userId);
            return Task.FromResult(accounts
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.Name)
                .ToList());
        }
    }

    public Task<AccountResponse> AddAccount(AccountCreateRequest req, string userId)
    {
        lock (gate)
        {
            if (accounts.Any(a => a.UserId == userId && a.Code == req.Code))
            {
                throw new InvalidOperationException("Account code already exists");
            }

            var account = new AccountResponse(
                Guid.NewGuid().ToString(),
                req.Code,
                req.Name,
                req.Type,
                req.Balance,
                req.IsSystem ?? false,
                userId);
            accounts.Add(account);
            return Task.FromResult(account);
        }
    }

    public Task UpdateAccount(string id, AccountPatchRequest patch, string userId)
    {
        lock (gate)
        {
            var index = accounts.FindIndex(a => a.Id == id && a.UserId == userId);
            if (index < 0) throw new Exception("Account not found or access denied");

            var account = accounts[index];
            accounts[index] = account with
            {
                Code = patch.Code ?? account.Code,
                Name = patch.Name ?? account.Name,
                Type = patch.Type ?? account.Type,
                Balance = patch.Balance ?? account.Balance,
                IsSystem = patch.IsSystem ?? account.IsSystem,
            };
            return Task.CompletedTask;
        }
    }

    public Task DeleteAccount(string id, string userId)
    {
        lock (gate)
        {
            accounts.RemoveAll(a => a.Id == id && a.UserId == userId);
            journalEntries.RemoveAll(e => e.UserId == userId && e.Lines.Any(l => l.AccountId == id));
            foreach (var movement in bankMovements.Where(m => m.UserId == userId && m.EntityId == id).ToList())
            {
                ReplaceMovement(movement with { EntityId = null });
            }
            return Task.CompletedTask;
        }
    }

    public Task<List<JournalEntryResponse>> GetJournalEntries(string userId)
    {
        lock (gate)
        {
            return Task.FromResult(journalEntries
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .ToList());
        }
    }

    public Task<JournalEntryResponse> AddJournalEntry(JournalEntryCreateRequest req, string userId)
    {
        lock (gate)
        {
            var entry = new JournalEntryResponse(
                Guid.NewGuid().ToString(),
                ParseDate(req.Date),
                req.Description,
                req.Lines.Select(l => new JournalLineResponse(
                    Guid.NewGuid().ToString(),
                    l.AccountId,
                    l.Credit,
                    l.Debit,
                    l.Description)).ToList(),
                userId);
            journalEntries.Add(entry);
            return Task.FromResult(entry);
        }
    }

    public Task UpdateJournalEntry(string id, JournalEntryPatchRequest patch, string userId)
    {
        lock (gate)
        {
            var index = journalEntries.FindIndex(e => e.Id == id && e.UserId == userId);
            if (index < 0) throw new Exception("Journal entry not found or access denied");

            var entry = journalEntries[index];
            journalEntries[index] = entry with
            {
                Date = patch.Date is null ? entry.Date : ParseDate(patch.Date),
                Description = patch.Description ?? entry.Description,
                Lines = patch.Lines is null
                    ? entry.Lines
                    : patch.Lines.Select(l => new JournalLineResponse(
                        Guid.NewGuid().ToString(),
                        l.AccountId,
                        l.Credit,
                        l.Debit,
                        l.Description)).ToList(),
            };
            return Task.CompletedTask;
        }
    }

    public Task DeleteJournalEntry(string id, string userId)
    {
        lock (gate)
        {
            journalEntries.RemoveAll(e => e.Id == id && e.UserId == userId);
            foreach (var movement in bankMovements.Where(m => m.UserId == userId && m.JournalEntryId == id).ToList())
            {
                ReplaceMovement(movement with { JournalEntryId = null });
            }
            return Task.CompletedTask;
        }
    }

    public Task<List<BankMovementResponse>> GetBankMovements(string userId)
    {
        lock (gate)
        {
            return Task.FromResult(bankMovements
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.Date)
                .ToList());
        }
    }

    public Task<BankMovementResponse> AddBankMovement(BankMovementCreateRequest req, string userId)
    {
        lock (gate)
        {
            var movement = new BankMovementResponse(
                Guid.NewGuid().ToString(),
                ParseDate(req.Date),
                req.Description,
                req.Amount,
                false,
                req.EntityId,
                req.JournalEntryId,
                userId);
            bankMovements.Add(movement);
            return Task.FromResult(movement);
        }
    }

    public Task UpdateBankMovement(string id, BankMovementPatchRequest patch, string userId)
    {
        lock (gate)
        {
            var movement = bankMovements.FirstOrDefault(m => m.Id == id && m.UserId == userId)
                ?? throw new Exception("Bank movement not found or access denied");
            ReplaceMovement(movement with
            {
                Date = patch.Date is null ? movement.Date : ParseDate(patch.Date),
                Description = patch.Description ?? movement.Description,
                Amount = patch.Amount ?? movement.Amount,
                IsIdentified = patch.IsIdentified ?? movement.IsIdentified,
                EntityId = patch.EntityId ?? movement.EntityId,
                JournalEntryId = patch.JournalEntryId ?? movement.JournalEntryId,
            });
            return Task.CompletedTask;
        }
    }

    public Task DeleteBankMovement(string id, string userId)
    {
        lock (gate)
        {
            bankMovements.RemoveAll(m => m.Id == id && m.UserId == userId);
            return Task.CompletedTask;
        }
    }

    private void EnsureSystemAccounts(string userId)
    {
        foreach (var (code, name, type) in SystemAccounts)
        {
            if (accounts.Any(a => a.UserId == userId && a.Code == code)) continue;
            accounts.Add(new AccountResponse(Guid.NewGuid().ToString(), code, name, type, 0m, true, userId));
        }
    }

    private void ReplaceMovement(BankMovementResponse replacement)
    {
        var index = bankMovements.FindIndex(m => m.Id == replacement.Id);
        if (index >= 0) bankMovements[index] = replacement;
    }

    private static DateTime ParseDate(string date) =>
        DateTime.Parse(date, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal);
}
