namespace CashClarity.Api.Models;

public class JournalLine
{
    public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; }
    public string AccountId { get; set; } = "";
    public decimal Credit { get; set; }
    public decimal Debit { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
