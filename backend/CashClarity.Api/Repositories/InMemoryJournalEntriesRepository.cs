using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;

namespace CashClarity.Api.Repositories;

public class InMemoryJournalEntriesRepository : IJournalEntriesRepository
{
    private readonly object gate = new();
    private readonly List<AccountResponse> accounts;
    private readonly List<JournalEntryResponse> journalEntries;
    private readonly List<BankMovementResponse> bankMovements;

    public InMemoryJournalEntriesRepository()
        : this([], [], [])
    {
    }

    public InMemoryJournalEntriesRepository(
        List<AccountResponse> accounts,
        List<JournalEntryResponse> journalEntries,
        List<BankMovementResponse> bankMovements)
    {
        this.accounts = accounts;
        this.journalEntries = journalEntries;
        this.bankMovements = bankMovements;
    }

    public Task<List<JournalEntryResponse>> GetJournalEntries(string userId, Guid workspaceId)
    {
        var ws = workspaceId.ToString();
        lock (gate)
        {
            return Task.FromResult(journalEntries
                .Where(e => e.WorkspaceId == ws)
                .OrderByDescending(e => e.Date)
                .ToList());
        }
    }

    public Task<JournalEntryResponse> AddJournalEntry(JournalEntryCreateRequest req, string userId, Guid workspaceId)
    {
        JournalEntryPolicy.EnsureBalanced(req.Lines.Select(l => (l.Debit, l.Credit)));

        var ws = workspaceId.ToString();
        lock (gate)
        {
            EnsureAccountsInWorkspace(req.Lines, ws);
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
                userId,
                ws);
            journalEntries.Add(entry);
            return Task.FromResult(entry);
        }
    }

    public Task UpdateJournalEntry(string id, JournalEntryPatchRequest patch, string userId, Guid workspaceId)
    {
        if (patch.Lines is not null)
        {
            JournalEntryPolicy.EnsureBalanced(patch.Lines.Select(l => (l.Debit, l.Credit)));
        }

        var ws = workspaceId.ToString();
        lock (gate)
        {
            var index = journalEntries.FindIndex(e => e.Id == id && e.WorkspaceId == ws);
            if (index < 0) throw new NotFoundException("Journal entry not found or access denied");
            if (patch.Lines is not null) EnsureAccountsInWorkspace(patch.Lines, ws);

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

    public Task DeleteJournalEntry(string id, string userId, Guid workspaceId)
    {
        var ws = workspaceId.ToString();
        lock (gate)
        {
            journalEntries.RemoveAll(e => e.Id == id && e.WorkspaceId == ws);
            for (var i = 0; i < bankMovements.Count; i++)
            {
                if (bankMovements[i].WorkspaceId == ws && bankMovements[i].JournalEntryId == id)
                {
                    bankMovements[i] = bankMovements[i] with { JournalEntryId = null };
                }
            }
            return Task.CompletedTask;
        }
    }

    private void EnsureAccountsInWorkspace(List<JournalLineRequest> lines, string ws)
    {
        if (lines.Any(l => !accounts.Any(a => a.Id == l.AccountId && a.WorkspaceId == ws)))
            throw new InvalidOperationException("Journal lines must reference accounts in the same workspace");
    }

    private static DateTime ParseDate(string date) =>
        DateTime.Parse(date, null,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal);
}
