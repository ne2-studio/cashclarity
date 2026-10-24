using CashClarity.Api.Domain;

namespace CashClarity.Api.Repositories;

public class InMemoryBankLinksRepository : IBankLinksRepository
{
    private readonly object gate = new();
    private readonly List<BankLink> links = [];

    public Task<BankLink?> GetBankLink(string userId)
    {
        lock (gate)
        {
            var link = links.FirstOrDefault(l => l.UserId == userId);
            return Task.FromResult(link is null ? null : Copy(link));
        }
    }

    public Task AddBankLink(BankLink link)
    {
        lock (gate)
        {
            if (links.Any(l => l.UserId == link.UserId))
                throw new InvalidOperationException("Bank link already exists for user");
            links.Add(Copy(link));
            return Task.CompletedTask;
        }
    }

    public Task UpdateBankLink(BankLink link)
    {
        lock (gate)
        {
            var index = links.FindIndex(l => l.Id == link.Id && l.UserId == link.UserId);
            if (index < 0) throw new NotFoundException("Bank link not found or access denied");
            link.UpdatedAt = DateTime.UtcNow;
            links[index] = Copy(link);
            return Task.CompletedTask;
        }
    }

    public Task DeleteBankLink(string userId)
    {
        lock (gate)
        {
            links.RemoveAll(l => l.UserId == userId);
            return Task.CompletedTask;
        }
    }

    private static BankLink Copy(BankLink link) => new BankLink
    {
        Id = link.Id,
        UserId = link.UserId,
        AspspName = link.AspspName,
        AspspCountry = link.AspspCountry,
        PsuType = link.PsuType,
        SyncFrom = link.SyncFrom,
        PendingState = link.PendingState,
        PendingStateExpiresAt = link.PendingStateExpiresAt,
        SessionId = link.SessionId,
        ValidUntil = link.ValidUntil,
        AccountOptionsJson = link.AccountOptionsJson,
        AccountUid = link.AccountUid,
        Iban = link.Iban,
        AccountName = link.AccountName,
        Currency = link.Currency,
        LastSyncedAt = link.LastSyncedAt,
        CreatedAt = link.CreatedAt,
        UpdatedAt = link.UpdatedAt,
    };
}
