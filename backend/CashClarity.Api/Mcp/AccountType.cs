using System.Text.Json.Serialization;

namespace CashClarity.Api.Mcp;

/// <summary>
/// Account types the app understands. The REST API stores them as free strings; the MCP schema
/// restricts them so agents cannot invent types the frontend would not render.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AccountType>))]
public enum AccountType
{
    [JsonStringEnumMemberName("main")] Main,
    [JsonStringEnumMemberName("space")] Space,
    [JsonStringEnumMemberName("entity")] Entity,
    [JsonStringEnumMemberName("uncategorized")] Uncategorized,
}

public static class AccountTypeExtensions
{
    public static string ToApiValue(this AccountType type) => type switch
    {
        AccountType.Main => "main",
        AccountType.Space => "space",
        AccountType.Entity => "entity",
        AccountType.Uncategorized => "uncategorized",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
