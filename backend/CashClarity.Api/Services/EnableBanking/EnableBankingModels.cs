namespace CashClarity.Api.Services.EnableBanking;

// Subconjunto del contrato de https://enablebanking.com/docs/api/reference/ que usa CashClarity.
// Se (de)serializa en snake_case.

public record EnableBankingAspsp(string Name, string Country, List<string>? PsuTypes, long? MaximumConsentValidity, string? Logo);

public record EnableBankingAspspList(List<EnableBankingAspsp> Aspsps);

public record EnableBankingAspspRef(string Name, string Country);

public record EnableBankingAccess(DateTimeOffset ValidUntil);

public record EnableBankingAuthRequest(
    EnableBankingAccess Access,
    EnableBankingAspspRef Aspsp,
    string State,
    string RedirectUrl,
    string PsuType);

public record EnableBankingAuthResponse(string Url, string? AuthorizationId);

public record EnableBankingSessionRequest(string Code);

public record EnableBankingAccountId(string? Iban);

public record EnableBankingAccount(string Uid, EnableBankingAccountId? AccountId, string? Name, string? Currency);

public record EnableBankingSession(string SessionId, List<EnableBankingAccount> Accounts, EnableBankingAccess Access);

public record EnableBankingAmount(string Amount, string? Currency);

public record EnableBankingParty(string? Name);

public record EnableBankingTransaction(
    string? EntryReference,
    string? TransactionId,
    EnableBankingAmount TransactionAmount,
    string? CreditDebitIndicator,
    string? Status,
    string? BookingDate,
    string? ValueDate,
    string? TransactionDate,
    List<string>? RemittanceInformation,
    EnableBankingParty? Creditor,
    EnableBankingParty? Debtor);

public record EnableBankingTransactionsPage(List<EnableBankingTransaction> Transactions, string? ContinuationKey);

public record EnableBankingBalance(string? Name, EnableBankingAmount BalanceAmount, string? BalanceType, string? ReferenceDate);

public record EnableBankingBalances(List<EnableBankingBalance> Balances);
