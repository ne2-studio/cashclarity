namespace CashClarity.Api.Models;

public class JournalEntry
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public string UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<JournalLine> Lines { get; set; } = [];
}
