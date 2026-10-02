namespace CashClarity.Api.Contracts;

public record AccountResponse(string Id, string Code, string Name, string Type, decimal Balance, bool IsSystem, string UserId);
public record AccountCreateRequest(string Code, string Name, string Type, decimal Balance = 0, bool? IsSystem = null);
public record AccountPatchRequest(string? Code = null, string? Name = null, string? Type = null, decimal? Balance = null, bool? IsSystem = null);
