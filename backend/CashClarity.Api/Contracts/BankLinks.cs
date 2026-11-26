namespace CashClarity.Api.Contracts;

public static class BankLinkStatus
{
    public const string None = "none";
    public const string Pending = "pending";
    public const string AwaitingAccount = "awaiting-account";
    public const string Active = "active";
    public const string Expired = "expired";
}

public record BankAccountOption(string Uid, string? Iban, string? Name, string? Currency);

public record BankLinkResponse(
    bool Configured,
    string Status,
    string? AspspName = null,
    string? AspspCountry = null,
    string? PsuType = null,
    string? Iban = null,
    string? AccountName = null,
    string? Currency = null,
    DateTime? ValidUntil = null,
    string? SyncFrom = null,
    DateTime? LastSyncedAt = null,
    List<BankAccountOption>? AccountOptions = null);

public record BankAspspResponse(string Name, string Country, List<string> PsuTypes, int? MaximumConsentValidityDays, string? Logo);

public record BankAuthorizationStartRequest(string AspspName, string AspspCountry, string PsuType, string? SyncFrom = null);
public record BankAuthorizationStartResponse(string Url);

public record BankAuthorizationCompleteRequest(string Code, string State);

public record BankAccountSelectRequest(string AccountUid);

public record BankBalanceResponse(decimal Amount, string? Currency, string? Type, string? ReferenceDate);

public record BankPossibleDuplicate(string MovementId, string ExistingMovementId);

public record BankSyncResponse(
    List<BankMovementResponse> Created,
    int SkippedExisting,
    List<BankPossibleDuplicate> PossibleDuplicates,
    string DateFrom,
    string DateTo,
    BankBalanceResponse? Balance,
    BankLinkResponse Link);
