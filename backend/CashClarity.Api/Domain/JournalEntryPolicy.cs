namespace CashClarity.Api.Domain;

/// <summary>
/// Thrown when a journal entry would violate the double-entry balance invariant
/// (Sum(Debit) != Sum(Credit) across its lines).
/// </summary>
public class UnbalancedJournalEntryException(decimal totalDebit, decimal totalCredit)
    : InvalidOperationException(
        $"Journal entry is not balanced: total debit {totalDebit} does not equal total credit {totalCredit}.");

/// <summary>
/// Canonical place deciding what a valid journal entry is, independent of storage backend.
/// </summary>
public static class JournalEntryPolicy
{
    public static void EnsureBalanced(IEnumerable<(decimal Debit, decimal Credit)> lines)
    {
        var totalDebit = 0m;
        var totalCredit = 0m;
        foreach (var (debit, credit) in lines)
        {
            totalDebit += debit;
            totalCredit += credit;
        }

        if (totalDebit != totalCredit)
        {
            throw new UnbalancedJournalEntryException(totalDebit, totalCredit);
        }
    }
}
