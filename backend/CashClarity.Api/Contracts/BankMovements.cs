namespace CashClarity.Api.Contracts;

public record BankMovementResponse(string Id, DateTime Date, string Description, decimal Amount, bool IsIdentified, string? EntityId, string? JournalEntryId, string UserId);
public record BankMovementCreateRequest(string Date, string Description, decimal Amount, string? EntityId = null, string? JournalEntryId = null);
public record BankMovementPatchRequest(string? Date = null, string? Description = null, decimal? Amount = null, bool? IsIdentified = null, string? EntityId = null, string? JournalEntryId = null);
