using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CashClarity.Api.Contracts;

namespace CashClarity.Api.Services.EnableBanking;

/// <summary>
/// Traduce transacciones de Enable Banking a movimientos de CashClarity con una clave de
/// deduplicación estable entre sincronizaciones.
/// </summary>
public static class EnableBankingTransactionMapper
{
    public static List<SyncedBankMovementCreate> Map(IEnumerable<EnableBankingTransaction> transactions)
    {
        var occurrences = new Dictionary<string, int>();
        var movements = new List<SyncedBankMovementCreate>();

        foreach (var transaction in transactions)
        {
            if (transaction.Status is not null && transaction.Status != "BOOK") continue;

            var date = ParseDate(transaction.BookingDate ?? transaction.ValueDate ?? transaction.TransactionDate);
            if (date is null) continue;

            var amount = SignedAmount(transaction);
            var description = Description(transaction);
            var baseKey = BaseKey(transaction, date.Value, amount, description);

            // Dos transacciones con la misma clave base dentro de una respuesta son movimientos distintos
            // (p. ej. dos cafés iguales el mismo día). El orden del banco dentro de un día es estable, así que
            // el índice de ocurrencia también lo es entre sincronizaciones que cubren el día completo.
            var occurrence = occurrences.GetValueOrDefault(baseKey);
            occurrences[baseKey] = occurrence + 1;
            var externalId = occurrence == 0 ? baseKey : $"{baseKey}#{occurrence}";

            movements.Add(new SyncedBankMovementCreate(externalId, date.Value, description, amount));
        }

        return movements;
    }

    private static string BaseKey(EnableBankingTransaction transaction, DateOnly date, decimal amount, string description)
    {
        if (!string.IsNullOrWhiteSpace(transaction.EntryReference)) return $"ref:{transaction.EntryReference.Trim()}";
        if (!string.IsNullOrWhiteSpace(transaction.TransactionId)) return $"tx:{transaction.TransactionId.Trim()}";

        var fingerprint = $"{date:yyyy-MM-dd}|{amount.ToString("0.00", CultureInfo.InvariantCulture)}|{Normalize(description)}";
        return $"hash:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)))}";
    }

    private static decimal SignedAmount(EnableBankingTransaction transaction)
    {
        var amount = Math.Abs(decimal.Parse(transaction.TransactionAmount.Amount, NumberStyles.Number, CultureInfo.InvariantCulture));
        return transaction.CreditDebitIndicator == "DBIT" ? -amount : amount;
    }

    private static string Description(EnableBankingTransaction transaction)
    {
        var remittance = string.Join(' ', (transaction.RemittanceInformation ?? [])
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim()));
        if (!string.IsNullOrWhiteSpace(remittance)) return remittance;

        var counterparty = transaction.CreditDebitIndicator == "DBIT" ? transaction.Creditor?.Name : transaction.Debtor?.Name;
        return string.IsNullOrWhiteSpace(counterparty) ? "Movimiento bancario" : counterparty.Trim();
    }

    private static string Normalize(string description) =>
        string.Join(' ', description.Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
}
