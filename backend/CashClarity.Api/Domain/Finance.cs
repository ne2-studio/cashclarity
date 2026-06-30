namespace CashClarity.Api.Domain;

// Response records (returned to client)
public record AccountResponse(string Id, string Code, string Name, string Type, decimal Balance, bool IsSystem, string UserId);
public record JournalLineResponse(string Id, string AccountId, decimal Credit, decimal Debit, string? Description);
public record JournalEntryResponse(string Id, DateTime Date, string? Description, List<JournalLineResponse> Lines, string UserId);
public record BankMovementResponse(string Id, DateTime Date, string Description, decimal Amount, bool IsIdentified, string? EntityId, string? JournalEntryId, string UserId);

// Request records (received from client)
public record AccountCreateRequest(string Code, string Name, string Type, decimal Balance = 0, bool? IsSystem = null);
public record AccountPatchRequest(string? Code = null, string? Name = null, string? Type = null, decimal? Balance = null, bool? IsSystem = null);

public record JournalLineRequest(string AccountId, decimal Credit, decimal Debit, string? Description = null);
public record JournalEntryCreateRequest(string Date, string? Description, List<JournalLineRequest> Lines);
public record JournalEntryPatchRequest(string? Date = null, string? Description = null, List<JournalLineRequest>? Lines = null);

public record BankMovementCreateRequest(string Date, string Description, decimal Amount, string? EntityId = null, string? JournalEntryId = null);
public record BankMovementPatchRequest(string? Date = null, string? Description = null, decimal? Amount = null, bool? IsIdentified = null, string? EntityId = null, string? JournalEntryId = null);
