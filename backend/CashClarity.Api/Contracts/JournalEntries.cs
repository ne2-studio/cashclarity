namespace CashClarity.Api.Contracts;

public record JournalLineResponse(string Id, string AccountId, decimal Credit, decimal Debit, string? Description);
public record JournalEntryResponse(string Id, DateTime Date, string? Description, List<JournalLineResponse> Lines, string UserId);
public record JournalLineRequest(string AccountId, decimal Credit, decimal Debit, string? Description = null);
public record JournalEntryCreateRequest(string Date, string? Description, List<JournalLineRequest> Lines);
public record JournalEntryPatchRequest(string? Date = null, string? Description = null, List<JournalLineRequest>? Lines = null);
