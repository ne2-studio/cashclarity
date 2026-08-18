using CashClarity.Api.Data;
using CashClarity.Api.Controllers;
using CashClarity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Repositories;

public interface IAccountsRepository
{
    Task<List<AccountResponse>> GetAccounts(string userId);
    Task<AccountResponse> AddAccount(AccountCreateRequest req, string userId);
    Task UpdateAccount(string id, AccountPatchRequest patch, string userId);
    Task DeleteAccount(string id, string userId);
}

public class AccountsRepository(FinanceDbContext db) : IAccountsRepository
{
    private static readonly (string Code, string Name, string Type)[] SystemAccounts =
    [
        ("0001", "Cuenta Principal", "main"),
        ("9999", "Sin categorizar", "uncategorized"),
    ];

    public async Task<List<AccountResponse>> GetAccounts(string userId)
    {
        await EnsureSystemAccounts(userId);
        return await db.Accounts
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.Name)
            .Select(a => new AccountResponse(a.Id, a.Code, a.Name, a.Type, a.Balance, a.IsSystem, userId))
            .ToListAsync();
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

    private static AccountResponse MapAccount(Account a, string userId) =>
        new(a.Id, a.Code, a.Name, a.Type, a.Balance, a.IsSystem, userId);
}
