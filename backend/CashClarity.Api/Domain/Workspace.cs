namespace CashClarity.Api.Domain;

public class Workspace
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string UserId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
