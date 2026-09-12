namespace CashClarity.Api.Domain;

/// <summary>
/// Thrown when an account cannot be deleted because it is still referenced by one or more
/// journal lines.
/// </summary>
public class AccountInUseException(string accountId)
    : InvalidOperationException(
        $"Account '{accountId}' cannot be deleted because it is still referenced by journal lines.");

/// <summary>
/// Canonical place deciding whether an account is safe to delete, independent of storage backend.
/// Product decision: deletion is blocked while journal lines still reference the account
/// (no cascading, no orphaning).
/// </summary>
public static class AccountDeletionPolicy
{
    public static void EnsureDeletable(string accountId, bool hasReferencingJournalLines)
    {
        if (hasReferencingJournalLines)
        {
            throw new AccountInUseException(accountId);
        }
    }
}
