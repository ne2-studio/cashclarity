namespace CashClarity.Api.Domain;

/// <summary>System accounts every workspace starts with.</summary>
public static class SystemAccountCatalog
{
    public static readonly (string Code, string Name, string Type)[] All =
    [
        ("0001", "Cuenta Principal", "main"),
        ("9999", "Sin categorizar", "uncategorized"),
    ];
}
