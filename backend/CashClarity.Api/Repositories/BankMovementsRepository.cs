using CashClarity.Api.Data;
using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Repositories;

public interface IBankMovementsRepository
{
    Task<List<BankMovementResponse>> GetBankMovements(string userId, Guid workspaceId);
    Task<BankMovementResponse> AddBankMovement(BankMovementCreateRequest req, string userId, Guid workspaceId);
    Task<List<BankMovementResponse>> AddBankMovements(List<BankMovementCreateRequest> requests, string userId, Guid workspaceId);
    Task UpdateBankMovement(string id, BankMovementPatchRequest patch, string userId, Guid workspaceId);
    Task DeleteBankMovement(string id, string userId, Guid workspaceId);
}

public class BankMovementsRepository(FinanceDbContext db) : IBankMovementsRepository
{
    public async Task<List<BankMovementResponse>> GetBankMovements(string userId, Guid workspaceId)
    {
        var movements = await db.BankMovements
            .Where(bm => bm.WorkspaceId == workspaceId)
            .OrderByDescending(bm => bm.Date)
            .ToListAsync();

        return movements.Select(MapBankMovement).ToList();
    }

    public async Task<BankMovementResponse> AddBankMovement(BankMovementCreateRequest req, string userId, Guid workspaceId)
    {
        await EnsureLinksInWorkspace([req], workspaceId);
        var movement = new BankMovement
        {
            Date = ParseDate(req.Date),
            Description = req.Description,
            Amount = req.Amount,
            EntityId = req.EntityId,
            JournalEntryId = req.JournalEntryId is null ? null : Guid.Parse(req.JournalEntryId),
            IsIdentified = false,
            UserId = userId,
            WorkspaceId = workspaceId,
        };
        db.BankMovements.Add(movement);
        await db.SaveChangesAsync();
        return MapBankMovement(movement);
    }

    public async Task<List<BankMovementResponse>> AddBankMovements(List<BankMovementCreateRequest> requests, string userId, Guid workspaceId)
    {
        await EnsureLinksInWorkspace(requests, workspaceId);
        var movements = requests.Select(req => new BankMovement
        {
            Date = ParseDate(req.Date),
            Description = req.Description,
            Amount = req.Amount,
            EntityId = req.EntityId,
            JournalEntryId = req.JournalEntryId is null ? null : Guid.Parse(req.JournalEntryId),
            IsIdentified = false,
            UserId = userId,
            WorkspaceId = workspaceId,
        }).ToList();

        db.BankMovements.AddRange(movements);
        await db.SaveChangesAsync();
        return movements.Select(MapBankMovement).ToList();
    }

    public async Task UpdateBankMovement(string id, BankMovementPatchRequest patch, string userId, Guid workspaceId)
    {
        var movement = await db.BankMovements
            .FirstOrDefaultAsync(bm => bm.Id == Guid.Parse(id) && bm.WorkspaceId == workspaceId)
            ?? throw new Exception("Bank movement not found or access denied");

        await EnsureLinksInWorkspace([new BankMovementCreateRequest("", "", 0, patch.EntityId, patch.JournalEntryId)], workspaceId);

        if (patch.Date is not null) movement.Date = ParseDate(patch.Date);
        if (patch.Description is not null) movement.Description = patch.Description;
        if (patch.Amount.HasValue) movement.Amount = patch.Amount.Value;
        if (patch.IsIdentified.HasValue) movement.IsIdentified = patch.IsIdentified.Value;
        if (patch.EntityId is not null) movement.EntityId = patch.EntityId;
        if (patch.JournalEntryId is not null) movement.JournalEntryId = Guid.Parse(patch.JournalEntryId);

        await db.SaveChangesAsync();
    }

    public async Task DeleteBankMovement(string id, string userId, Guid workspaceId)
    {
        await db.BankMovements
            .Where(bm => bm.Id == Guid.Parse(id) && bm.WorkspaceId == workspaceId)
            .ExecuteDeleteAsync();
    }

    private async Task EnsureLinksInWorkspace(List<BankMovementCreateRequest> requests, Guid workspaceId)
    {
        var entityIds = requests.Where(r => r.EntityId is not null).Select(r => r.EntityId!).Distinct().ToList();
        if (entityIds.Count > 0 &&
            await db.Accounts.CountAsync(a => a.WorkspaceId == workspaceId && entityIds.Contains(a.Id)) != entityIds.Count)
            throw new InvalidOperationException("Entity must belong to the same workspace");

        var entryIds = requests.Where(r => r.JournalEntryId is not null).Select(r => Guid.Parse(r.JournalEntryId!)).Distinct().ToList();
        if (entryIds.Count > 0 &&
            await db.JournalEntries.CountAsync(e => e.WorkspaceId == workspaceId && entryIds.Contains(e.Id)) != entryIds.Count)
            throw new InvalidOperationException("Journal entry must belong to the same workspace");
    }

    private static BankMovementResponse MapBankMovement(BankMovement bm) =>
        new(bm.Id.ToString(), bm.Date, bm.Description, bm.Amount, bm.IsIdentified,
            bm.EntityId, bm.JournalEntryId?.ToString(), bm.UserId, bm.WorkspaceId.ToString());

    private static DateTime ParseDate(string date) =>
        DateTime.Parse(date, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal);
}
