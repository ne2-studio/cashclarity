namespace CashClarity.Api.Domain;

public class BankMovement
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public bool IsIdentified { get; set; }
    public string? EntityId { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string UserId { get; set; } = "";

    /// <summary>Clave estable del movimiento en el banco; null para movimientos manuales o de CSV.</summary>
    public string? ExternalId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
