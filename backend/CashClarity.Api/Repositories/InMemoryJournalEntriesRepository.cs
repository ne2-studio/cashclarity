using CashClarity.Api.Domain;

namespace CashClarity.Api.Repositories;

public class InMemoryJournalEntriesRepository : IJournalEntriesRepository
{
    private readonly object gate = new();
    private readonly List<JournalEntryResponse> journalEntries;
    private readonly List<BankMovementResponse> bankMovements;

    public InMemoryJournalEntriesRepository()
        : this([], [])
    {
    }

    public InMemoryJournalEntriesRepository(
        List<JournalEntryResponse> journalEntries,
        List<BankMovementResponse> bankMovements)
    {
        this.journalEntries = journalEntries;
        this.bankMovements = bankMovements;
    }

    public Task<List<JournalEntryResponse>> GetJournalEntries(string userId)
    {
        lock (gate)
        {
            return Task.FromResult(journalEntries
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date)
                .ToList());
        }
    }

    public Task<JournalEntryResponse> AddJournalEntry(JournalEntryCreateRequest req, string userId)
    {
        lock (gate)
        {
            var entry = new JournalEntryResponse(
                Guid.NewGuid().ToString(),
                ParseDate(req.Date),
                req.Description,
                req.Lines.Select(l => new JournalLineResponse(
                    Guid.NewGuid().ToString(),
                    l.AccountId,
                    l.Credit,
                    l.Debit,
                    l.Description)).ToList(),
                userId);
            journalEntries.Add(entry);
            return Task.FromResult(entry);
        }
    }

    public Task UpdateJournalEntry(string id, JournalEntryPatchRequest patch, string userId)
    {
        lock (gate)
        {
            var index = journalEntries.FindIndex(e => e.Id == id && e.UserId == userId);
            if (index < 0) throw new Exception("Journal entry not found or access denied");

            var entry = journalEntries[index];
            journalEntries[index] = entry with
            {
                Date = patch.Date is null ? entry.Date : ParseDate(patch.Date),
                Description = patch.Description ?? entry.Description,
                Lines = patch.Lines is null
                    ? entry.Lines
                    : patch.Lines.Select(l => new JournalLineResponse(
                        Guid.NewGuid().ToString(),
                        l.AccountId,
                        l.Credit,
                        l.Debit,
                        l.Description)).ToList(),
            };
            return Task.CompletedTask;
        }
    }

    public Task DeleteJournalEntry(string id, string userId)
    {
        lock (gate)
        {
            journalEntries.RemoveAll(e => e.Id == id && e.UserId == userId);
            for (var i = 0; i < bankMovements.Count; i++)
            {
                if (bankMovements[i].UserId == userId && bankMovements[i].JournalEntryId == id)
                {
                    bankMovements[i] = bankMovements[i] with { JournalEntryId = null };
                }
            }
            return Task.CompletedTask;
        }
    }

    private static DateTime ParseDate(string date) =>
        DateTime.Parse(date, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal);
}
