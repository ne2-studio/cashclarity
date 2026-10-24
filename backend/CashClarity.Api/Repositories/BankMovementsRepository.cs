using CashClarity.Api.Data;
using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Repositories;

public interface IBankMovementsRepository
{
    Task<List<BankMovementResponse>> GetBankMovements(string userId);
    Task<BankMovementResponse> AddBankMovement(BankMovementCreateRequest req, string userId);
    Task<List<BankMovementResponse>> AddBankMovements(List<BankMovementCreateRequest> requests, string userId);

    /// <summary>Inserta los movimientos cuyo ExternalId aún no existe para el usuario y devuelve solo los creados.</summary>
    Task<List<BankMovementResponse>> AddSyncedBankMovements(List<SyncedBankMovementCreate> movements, string userId);
    Task UpdateBankMovement(string id, BankMovementPatchRequest patch, string userId);
    Task DeleteBankMovement(string id, string userId);
}

public class BankMovementsRepository(FinanceDbContext db) : IBankMovementsRepository
{
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

    public async Task<List<BankMovementResponse>> AddBankMovements(List<BankMovementCreateRequest> requests, string userId)
    {
        var movements = requests.Select(req => new BankMovement
        {
            Date = ParseDate(req.Date),
            Description = req.Description,
            Amount = req.Amount,
            EntityId = req.EntityId,
            JournalEntryId = req.JournalEntryId is null ? null : Guid.Parse(req.JournalEntryId),
            IsIdentified = false,
            UserId = userId,
        }).ToList();

        db.BankMovements.AddRange(movements);
        await db.SaveChangesAsync();
        return movements.Select(MapBankMovement).ToList();
    }

    public async Task<List<BankMovementResponse>> AddSyncedBankMovements(List<SyncedBankMovementCreate> movements, string userId)
    {
        var candidateIds = movements.Select(m => m.ExternalId).ToList();
        var existingIds = await db.BankMovements
            .Where(bm => bm.UserId == userId && bm.ExternalId != null && candidateIds.Contains(bm.ExternalId))
            .Select(bm => bm.ExternalId!)
            .ToListAsync();
        var known = existingIds.ToHashSet();

        var created = movements
            .Where(m => known.Add(m.ExternalId))
            .Select(m => new BankMovement
            {
                Date = m.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Description = m.Description,
                Amount = m.Amount,
                IsIdentified = false,
                UserId = userId,
                ExternalId = m.ExternalId,
            })
            .ToList();

        db.BankMovements.AddRange(created);
        await db.SaveChangesAsync();
        return created.Select(MapBankMovement).ToList();
    }

    public async Task UpdateBankMovement(string id, BankMovementPatchRequest patch, string userId)
    {
        var movement = await db.BankMovements
            .FirstOrDefaultAsync(bm => bm.Id == Guid.Parse(id) && bm.UserId == userId)
            ?? throw new NotFoundException("Bank movement not found or access denied");

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

    private static BankMovementResponse MapBankMovement(BankMovement bm) =>
        new(bm.Id.ToString(), bm.Date, bm.Description, bm.Amount, bm.IsIdentified,
            bm.EntityId, bm.JournalEntryId?.ToString(), bm.UserId.ToString(), bm.ExternalId);

    private static DateTime ParseDate(string date) =>
        DateTime.Parse(date, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal);
}
