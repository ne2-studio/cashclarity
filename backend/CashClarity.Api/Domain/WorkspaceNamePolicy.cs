namespace CashClarity.Api.Domain;

public static class WorkspaceNamePolicy
{
    public const int MaxLength = 100;

    public static string Normalize(string? name)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0) throw new InvalidOperationException("Workspace name is required");
        if (trimmed.Length > MaxLength)
            throw new InvalidOperationException($"Workspace name must be at most {MaxLength} characters");
        return trimmed;
    }
}
