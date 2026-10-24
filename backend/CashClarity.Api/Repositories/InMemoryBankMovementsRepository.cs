using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;

namespace CashClarity.Api.Repositories;

public class InMemoryBankMovementsRepository : IBankMovementsRepository
{
    private readonly object gate = new();
    private readonly List<BankMovementResponse> bankMovements;

    public InMemoryBankMovementsRepository()
        : this([])
    {
    }

    public InMemoryBankMovementsRepository(List<BankMovementResponse> bankMovements)
    {
        this.bankMovements = bankMovements;
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

    public Task<List<BankMovementResponse>> AddBankMovements(List<BankMovementCreateRequest> requests, string userId)
    {
        lock (gate)
        {
            var created = requests.Select(req => new BankMovementResponse(
                Guid.NewGuid().ToString(),
                ParseDate(req.Date),
                req.Description,
                req.Amount,
                false,
                req.EntityId,
                req.JournalEntryId,
                userId)).ToList();
            bankMovements.AddRange(created);
            return Task.FromResult(created);
        }
    }

    public Task<List<BankMovementResponse>> AddSyncedBankMovements(List<SyncedBankMovementCreate> movements, string userId)
    {
        lock (gate)
        {
            var known = bankMovements
                .Where(m => m.UserId == userId && m.ExternalId is not null)
                .Select(m => m.ExternalId!)
                .ToHashSet();
            var created = movements
                .Where(m => known.Add(m.ExternalId))
                .Select(m => new BankMovementResponse(
                    Guid.NewGuid().ToString(),
                    m.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    m.Description,
                    m.Amount,
                    false,
                    null,
                    null,
                    userId,
                    m.ExternalId))
                .ToList();
            bankMovements.AddRange(created);
            return Task.FromResult(created);
        }
    }

    public Task UpdateBankMovement(string id, BankMovementPatchRequest patch, string userId)
    {
        lock (gate)
        {
            var index = bankMovements.FindIndex(m => m.Id == id && m.UserId == userId);
            if (index < 0) throw new NotFoundException("Bank movement not found or access denied");

            var movement = bankMovements[index];
            bankMovements[index] = movement with
            {
                Date = patch.Date is null ? movement.Date : ParseDate(patch.Date),
                Description = patch.Description ?? movement.Description,
                Amount = patch.Amount ?? movement.Amount,
                IsIdentified = patch.IsIdentified ?? movement.IsIdentified,
                EntityId = patch.EntityId ?? movement.EntityId,
                JournalEntryId = patch.JournalEntryId ?? movement.JournalEntryId,
            };
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

    private static DateTime ParseDate(string date) =>
        DateTime.Parse(date, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal);
}
