using CashClarity.Api.Data;
using CashClarity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Repositories;

public interface IBankLinksRepository
{
    Task<BankLink?> GetBankLink(Guid workspaceId);
    Task AddBankLink(BankLink link);
    Task UpdateBankLink(BankLink link);
    Task DeleteBankLink(Guid workspaceId);
}

public class BankLinksRepository(FinanceDbContext db) : IBankLinksRepository
{
    public Task<BankLink?> GetBankLink(Guid workspaceId) =>
        db.BankLinks.FirstOrDefaultAsync(bl => bl.WorkspaceId == workspaceId);

    public async Task AddBankLink(BankLink link)
    {
        db.BankLinks.Add(link);
        await db.SaveChangesAsync();
    }

    public async Task UpdateBankLink(BankLink link)
    {
        link.UpdatedAt = DateTime.UtcNow;
        if (db.Entry(link).State == EntityState.Detached) db.BankLinks.Update(link);
        await db.SaveChangesAsync();
    }

    public async Task DeleteBankLink(Guid workspaceId)
    {
        await db.BankLinks.Where(bl => bl.WorkspaceId == workspaceId).ExecuteDeleteAsync();
    }
}
