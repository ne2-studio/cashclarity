using CashClarity.Api.Data;
using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Repositories;

public interface IJournalEntriesRepository
{
    Task<List<JournalEntryResponse>> GetJournalEntries(string userId, Guid workspaceId);
    Task<JournalEntryResponse> AddJournalEntry(JournalEntryCreateRequest req, string userId, Guid workspaceId);
    Task UpdateJournalEntry(string id, JournalEntryPatchRequest patch, string userId, Guid workspaceId);
    Task DeleteJournalEntry(string id, string userId, Guid workspaceId);
}

public class JournalEntriesRepository(FinanceDbContext db) : IJournalEntriesRepository
{
    public async Task<List<JournalEntryResponse>> GetJournalEntries(string userId, Guid workspaceId)
    {
        var entries = await db.JournalEntries
            .Include(je => je.Lines)
            .Where(je => je.WorkspaceId == workspaceId)
            .OrderByDescending(je => je.Date)
            .ToListAsync();

        return entries.Select(e => MapEntry(e)).ToList();
    }

    public async Task<JournalEntryResponse> AddJournalEntry(JournalEntryCreateRequest req, string userId, Guid workspaceId)
    {
        JournalEntryPolicy.EnsureBalanced(req.Lines.Select(l => (l.Debit, l.Credit)));
        await EnsureAccountsInWorkspace(req.Lines, workspaceId);

        var entry = new JournalEntry
        {
            Date = ParseDate(req.Date),
            Description = req.Description,
            UserId = userId,
            WorkspaceId = workspaceId,
            Lines = req.Lines.Select(l => new JournalLine
            {
                AccountId = l.AccountId,
                Credit = l.Credit,
                Debit = l.Debit,
                Description = l.Description,
            }).ToList(),
        };
        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync();
        return MapEntry(entry);
    }

    public async Task UpdateJournalEntry(string id, JournalEntryPatchRequest patch, string userId, Guid workspaceId)
    {
        var entryId = Guid.Parse(id);
        var entry = await db.JournalEntries
            .Include(je => je.Lines)
            .FirstOrDefaultAsync(je => je.Id == entryId && je.WorkspaceId == workspaceId)
            ?? throw new NotFoundException("Journal entry not found or access denied");

        if (patch.Date is not null) entry.Date = ParseDate(patch.Date);
        if (patch.Description is not null) entry.Description = patch.Description;

        if (patch.Lines is not null)
        {
            JournalEntryPolicy.EnsureBalanced(patch.Lines.Select(l => (l.Debit, l.Credit)));
            await EnsureAccountsInWorkspace(patch.Lines, workspaceId);

            entry.Lines.Clear();
            foreach (var l in patch.Lines)
            {
                entry.Lines.Add(new JournalLine
                {
                    AccountId = l.AccountId,
                    Credit = l.Credit,
                    Debit = l.Debit,
                    Description = l.Description,
                });
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteJournalEntry(string id, string userId, Guid workspaceId)
    {
        await db.JournalEntries
            .Where(je => je.Id == Guid.Parse(id) && je.WorkspaceId == workspaceId)
            .ExecuteDeleteAsync();
    }

    private async Task EnsureAccountsInWorkspace(List<JournalLineRequest> lines, Guid workspaceId)
    {
        var accountIds = lines.Select(l => l.AccountId).Distinct().ToList();
        var found = await db.Accounts
            .CountAsync(a => a.WorkspaceId == workspaceId && accountIds.Contains(a.Id));
        if (found != accountIds.Count)
            throw new InvalidOperationException("Journal lines must reference accounts in the same workspace");
    }

    private static JournalLineResponse MapLine(JournalLine l) =>
        new(l.Id.ToString(), l.AccountId, l.Credit, l.Debit, l.Description);

    private static JournalEntryResponse MapEntry(JournalEntry e) =>
        new(e.Id.ToString(), e.Date, e.Description, e.Lines.Select(MapLine).ToList(), e.UserId, e.WorkspaceId.ToString());

    private static DateTime ParseDate(string date) =>
        DateTime.Parse(date, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal);
}
