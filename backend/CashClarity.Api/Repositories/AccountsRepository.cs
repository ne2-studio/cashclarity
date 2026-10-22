using CashClarity.Api.Data;
using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Repositories;

public interface IAccountsRepository
{
    Task<List<AccountResponse>> GetAccounts(string userId, Guid workspaceId);
    Task<AccountResponse> AddAccount(AccountCreateRequest req, string userId, Guid workspaceId);
    Task UpdateAccount(string id, AccountPatchRequest patch, string userId, Guid workspaceId);
    Task DeleteAccount(string id, string userId, Guid workspaceId);
}

public class AccountsRepository(FinanceDbContext db) : IAccountsRepository
{
    public async Task<List<AccountResponse>> GetAccounts(string userId, Guid workspaceId)
    {
        await EnsureSystemAccounts(userId, workspaceId);
        return await db.Accounts
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderBy(a => a.Name)
            .Select(a => new AccountResponse(a.Id, a.Code, a.Name, a.Type, a.Balance, a.IsSystem, userId, workspaceId.ToString()))
            .ToListAsync();
    }

    public async Task<AccountResponse> AddAccount(AccountCreateRequest req, string userId, Guid workspaceId)
    {
        var account = new Account
        {
            Code = req.Code,
            Name = req.Name,
            Type = req.Type,
            Balance = req.Balance,
            IsSystem = req.IsSystem ?? false,
            UserId = userId,
            WorkspaceId = workspaceId,
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return MapAccount(account);
    }

    public async Task UpdateAccount(string id, AccountPatchRequest patch, string userId, Guid workspaceId)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.WorkspaceId == workspaceId)
            ?? throw new NotFoundException("Account not found or access denied");

        if (patch.Code is not null) account.Code = patch.Code;
        if (patch.Name is not null) account.Name = patch.Name;
        if (patch.Type is not null) account.Type = patch.Type;
        if (patch.Balance.HasValue) account.Balance = patch.Balance.Value;
        if (patch.IsSystem.HasValue) account.IsSystem = patch.IsSystem.Value;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAccount(string id, string userId, Guid workspaceId)
    {
        var account = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == id && a.WorkspaceId == workspaceId);
        if (account is null) return;

        var hasReferencingJournalLines = await db.JournalLines.AnyAsync(l => l.AccountId == id);
        AccountDeletionPolicy.EnsureDeletable(id, hasReferencingJournalLines);

        db.Accounts.Remove(account);
        await db.SaveChangesAsync();
    }

    private async Task EnsureSystemAccounts(string userId, Guid workspaceId)
    {
        foreach (var (code, name, type) in SystemAccountCatalog.All)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO accounts (code, name, type, is_system, balance, user_id, workspace_id)
                VALUES ({code}, {name}, {type}, true, 0, {userId}, {workspaceId})
                ON CONFLICT (workspace_id, code) DO NOTHING
                """);
        }
    }

    private static AccountResponse MapAccount(Account a) =>
        new(a.Id, a.Code, a.Name, a.Type, a.Balance, a.IsSystem, a.UserId, a.WorkspaceId.ToString());
}
