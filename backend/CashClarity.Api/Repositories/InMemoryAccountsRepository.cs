using CashClarity.Api.Controllers;

namespace CashClarity.Api.Repositories;

public class InMemoryAccountsRepository : IAccountsRepository
{
    private static readonly (string Code, string Name, string Type)[] SystemAccounts =
    [
        ("0001", "Cuenta Principal", "main"),
        ("9999", "Sin categorizar", "uncategorized"),
    ];

    private readonly object gate = new();
    private readonly List<AccountResponse> accounts = [];
    private readonly List<JournalEntryResponse> journalEntries;
    private readonly List<BankMovementResponse> bankMovements;

    public InMemoryAccountsRepository()
        : this([], [])
    {
    }

    public InMemoryAccountsRepository(
        List<JournalEntryResponse> journalEntries,
        List<BankMovementResponse> bankMovements)
    {
        this.journalEntries = journalEntries;
        this.bankMovements = bankMovements;
    }

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
            for (var i = 0; i < bankMovements.Count; i++)
            {
                if (bankMovements[i].UserId == userId && bankMovements[i].EntityId == id)
                {
                    bankMovements[i] = bankMovements[i] with { EntityId = null };
                }
            }
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
}
