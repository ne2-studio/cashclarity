using CashClarity.Api.Data;
using CashClarity.Api.Domain;
using CashClarity.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Repositories;

public interface IBankMovementsRepository
{
    Task<List<BankMovementResponse>> GetBankMovements(string userId);
    Task<BankMovementResponse> AddBankMovement(BankMovementCreateRequest req, string userId);
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

    public async Task UpdateBankMovement(string id, BankMovementPatchRequest patch, string userId)
    {
        var movement = await db.BankMovements
            .FirstOrDefaultAsync(bm => bm.Id == Guid.Parse(id) && bm.UserId == userId)
            ?? throw new Exception("Bank movement not found or access denied");

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
            bm.EntityId, bm.JournalEntryId?.ToString(), bm.UserId.ToString());

    private static DateTime ParseDate(string date) =>
        DateTime.Parse(date, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal);
}
