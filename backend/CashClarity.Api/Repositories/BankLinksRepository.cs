using CashClarity.Api.Data;
using CashClarity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Repositories;

public interface IBankLinksRepository
{
    Task<BankLink?> GetBankLink(string userId);
    Task AddBankLink(BankLink link);
    Task UpdateBankLink(BankLink link);
    Task DeleteBankLink(string userId);
}

public class BankLinksRepository(FinanceDbContext db) : IBankLinksRepository
{
    public Task<BankLink?> GetBankLink(string userId) =>
        db.BankLinks.FirstOrDefaultAsync(bl => bl.UserId == userId);

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

    public async Task DeleteBankLink(string userId)
    {
        await db.BankLinks.Where(bl => bl.UserId == userId).ExecuteDeleteAsync();
    }
}
