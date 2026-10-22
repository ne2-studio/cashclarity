using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;

namespace CashClarity.Api.Repositories;

public class InMemoryBankMovementsRepository : IBankMovementsRepository
{
    private readonly object gate = new();
    private readonly List<BankMovementResponse> bankMovements;
    private readonly List<AccountResponse> accounts;
    private readonly List<JournalEntryResponse> journalEntries;

    public InMemoryBankMovementsRepository()
        : this([], [], [])
    {
    }

    public InMemoryBankMovementsRepository(
        List<BankMovementResponse> bankMovements,
        List<AccountResponse> accounts,
        List<JournalEntryResponse> journalEntries)
    {
        this.bankMovements = bankMovements;
        this.accounts = accounts;
        this.journalEntries = journalEntries;
    }

    public Task<List<BankMovementResponse>> GetBankMovements(string userId, Guid workspaceId)
    {
        var ws = workspaceId.ToString();
        lock (gate)
        {
            return Task.FromResult(bankMovements
                .Where(m => m.WorkspaceId == ws)
                .OrderByDescending(m => m.Date)
                .ToList());
        }
    }

    public Task<BankMovementResponse> AddBankMovement(BankMovementCreateRequest req, string userId, Guid workspaceId)
    {
        var ws = workspaceId.ToString();
        lock (gate)
        {
            EnsureLinksInWorkspace(req.EntityId, req.JournalEntryId, ws);
            var movement = new BankMovementResponse(
                Guid.NewGuid().ToString(),
                ParseDate(req.Date),
                req.Description,
                req.Amount,
                false,
                req.EntityId,
                req.JournalEntryId,
                userId,
                ws);
            bankMovements.Add(movement);
            return Task.FromResult(movement);
        }
    }

    public Task<List<BankMovementResponse>> AddBankMovements(List<BankMovementCreateRequest> requests, string userId, Guid workspaceId)
    {
        var ws = workspaceId.ToString();
        lock (gate)
        {
            foreach (var r in requests) EnsureLinksInWorkspace(r.EntityId, r.JournalEntryId, ws);
            var created = requests.Select(req => new BankMovementResponse(
                Guid.NewGuid().ToString(),
                ParseDate(req.Date),
                req.Description,
                req.Amount,
                false,
                req.EntityId,
                req.JournalEntryId,
                userId,
                ws)).ToList();
            bankMovements.AddRange(created);
            return Task.FromResult(created);
        }
    }

    public Task UpdateBankMovement(string id, BankMovementPatchRequest patch, string userId, Guid workspaceId)
    {
        var ws = workspaceId.ToString();
        lock (gate)
        {
            var index = bankMovements.FindIndex(m => m.Id == id && m.WorkspaceId == ws);
            if (index < 0) throw new NotFoundException("Bank movement not found or access denied");

            EnsureLinksInWorkspace(patch.EntityId, patch.JournalEntryId, ws);
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

    public Task DeleteBankMovement(string id, string userId, Guid workspaceId)
    {
        var ws = workspaceId.ToString();
        lock (gate)
        {
            bankMovements.RemoveAll(m => m.Id == id && m.WorkspaceId == ws);
            return Task.CompletedTask;
        }
    }

    private void EnsureLinksInWorkspace(string? entityId, string? journalEntryId, string ws)
    {
        if (entityId is not null && !accounts.Any(a => a.Id == entityId && a.WorkspaceId == ws))
            throw new InvalidOperationException("Entity must belong to the same workspace");
        if (journalEntryId is not null && !journalEntries.Any(e => e.Id == journalEntryId && e.WorkspaceId == ws))
            throw new InvalidOperationException("Journal entry must belong to the same workspace");
    }

    private static DateTime ParseDate(string date) =>
        DateTime.Parse(date, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal);
}
