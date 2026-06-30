namespace CashClarity.Api.Models;

public class Account
{
    public string Id { get; set; } = null!;
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal Balance { get; set; }
    public bool IsSystem { get; set; }
    public string UserId { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
