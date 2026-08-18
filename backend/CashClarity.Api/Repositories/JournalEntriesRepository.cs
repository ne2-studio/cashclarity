using CashClarity.Api.Data;
using CashClarity.Api.Domain;
using CashClarity.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CashClarity.Api.Repositories;

public interface IJournalEntriesRepository
{
    Task<List<JournalEntryResponse>> GetJournalEntries(string userId);
    Task<JournalEntryResponse> AddJournalEntry(JournalEntryCreateRequest req, string userId);
    Task UpdateJournalEntry(string id, JournalEntryPatchRequest patch, string userId);
    Task DeleteJournalEntry(string id, string userId);
}

public class JournalEntriesRepository(FinanceDbContext db) : IJournalEntriesRepository
{
    public async Task<List<JournalEntryResponse>> GetJournalEntries(string userId)
    {
        var entries = await db.JournalEntries
            .Include(je => je.Lines)
            .Where(je => je.UserId == userId)
            .OrderByDescending(je => je.Date)
            .ToListAsync();

        return entries.Select(e => MapEntry(e, userId)).ToList();
    }

    public async Task<JournalEntryResponse> AddJournalEntry(JournalEntryCreateRequest req, string userId)
    {
        var entry = new JournalEntry
        {
            Date = ParseDate(req.Date),
            Description = req.Description,
            UserId = userId,
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
        return MapEntry(entry, userId);
    }

    public async Task UpdateJournalEntry(string id, JournalEntryPatchRequest patch, string userId)
    {
        var entryId = Guid.Parse(id);
        var entry = await db.JournalEntries
            .Include(je => je.Lines)
            .FirstOrDefaultAsync(je => je.Id == entryId && je.UserId == userId)
            ?? throw new Exception("Journal entry not found or access denied");

        if (patch.Date is not null) entry.Date = ParseDate(patch.Date);
        if (patch.Description is not null) entry.Description = patch.Description;

        if (patch.Lines is not null)
        {
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

    public async Task DeleteJournalEntry(string id, string userId)
    {
        await db.JournalEntries
            .Where(je => je.Id == Guid.Parse(id) && je.UserId == userId)
            .ExecuteDeleteAsync();
    }

    private static JournalLineResponse MapLine(JournalLine l) =>
        new(l.Id.ToString(), l.AccountId, l.Credit, l.Debit, l.Description);

    private static JournalEntryResponse MapEntry(JournalEntry e, string userId) =>
        new(e.Id.ToString(), e.Date, e.Description, e.Lines.Select(MapLine).ToList(), userId);

    private static DateTime ParseDate(string date) =>
        DateTime.Parse(date, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal);
}
