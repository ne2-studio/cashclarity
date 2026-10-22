namespace CashClarity.Api.Contracts;

public record WorkspaceResponse(string Id, string Name, DateTime CreatedAt);
public record WorkspaceCreateRequest(string? Name);
