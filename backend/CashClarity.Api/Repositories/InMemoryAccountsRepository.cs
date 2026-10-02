using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;

namespace CashClarity.Api.Repositories;

public class InMemoryAccountsRepository : IAccountsRepository
{
    private readonly object gate = new();
    private readonly List<AccountResponse> accounts;
    private readonly List<JournalEntryResponse> journalEntries;
    private readonly List<BankMovementResponse> bankMovements;

    public InMemoryAccountsRepository()
        : this([], [], [])
    {
    }

    public InMemoryAccountsRepository(
        List<AccountResponse> accounts,
        List<JournalEntryResponse> journalEntries,
        List<BankMovementResponse> bankMovements)
    {
        this.accounts = accounts;
        this.journalEntries = journalEntries;
        this.bankMovements = bankMovements;
    }

    public Task<List<AccountResponse>> GetAccounts(string userId, Guid workspaceId)
    {
        var ws = workspaceId.ToString();
        lock (gate)
        {
            EnsureSystemAccounts(userId, workspaceId);
            return Task.FromResult(accounts
                .Where(a => a.WorkspaceId == ws)
                .OrderBy(a => a.Name)
                .ToList());
        }
    }

    public Task<AccountResponse> AddAccount(AccountCreateRequest req, string userId, Guid workspaceId)
    {
        var ws = workspaceId.ToString();
        lock (gate)
        {
            if (accounts.Any(a => a.WorkspaceId == ws && a.Code == req.Code))
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
                userId,
                ws);
            accounts.Add(account);
            return Task.FromResult(account);
        }
    }

    public Task UpdateAccount(string id, AccountPatchRequest patch, string userId, Guid workspaceId)
    {
        var ws = workspaceId.ToString();
        lock (gate)
        {
            var index = accounts.FindIndex(a => a.Id == id && a.WorkspaceId == ws);
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

    public Task DeleteAccount(string id, string userId, Guid workspaceId)
    {
        var ws = workspaceId.ToString();
        lock (gate)
        {
            var account = accounts.FirstOrDefault(a => a.Id == id && a.WorkspaceId == ws);
            if (account is null) return Task.CompletedTask;

            var hasReferencingJournalLines = journalEntries
                .Any(e => e.WorkspaceId == ws && e.Lines.Any(l => l.AccountId == id));
            AccountDeletionPolicy.EnsureDeletable(id, hasReferencingJournalLines);

            accounts.Remove(account);
            for (var i = 0; i < bankMovements.Count; i++)
            {
                if (bankMovements[i].WorkspaceId == ws && bankMovements[i].EntityId == id)
                {
                    bankMovements[i] = bankMovements[i] with { EntityId = null };
                }
            }
            return Task.CompletedTask;
        }
    }

    private void EnsureSystemAccounts(string userId, Guid workspaceId)
    {
        var ws = workspaceId.ToString();
        foreach (var (code, name, type) in SystemAccountCatalog.All)
        {
            if (accounts.Any(a => a.WorkspaceId == ws && a.Code == code)) continue;
            accounts.Add(new AccountResponse(Guid.NewGuid().ToString(), code, name, type, 0m, true, userId, ws));
        }
    }
}
