namespace CashClarity.Api.Domain;

/// <summary>
/// Vínculo entre el libro de un usuario y una cuenta bancaria real leída vía Enable Banking.
/// Hay como máximo uno por libro (hoy, por usuario).
/// </summary>
public class BankLink
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public string AspspName { get; set; } = "";
    public string AspspCountry { get; set; } = "";
    public string PsuType { get; set; } = "";
    public DateOnly SyncFrom { get; set; }

    /// <summary>State anti-CSRF de una autorización en curso; null cuando no hay ninguna.</summary>
    public string? PendingState { get; set; }
    public DateTime? PendingStateExpiresAt { get; set; }

    public string? SessionId { get; set; }
    public DateTime? ValidUntil { get; set; }

    /// <summary>Cuentas devueltas por la sesión, en JSON, mientras el usuario elige una.</summary>
    public string? AccountOptionsJson { get; set; }

    public string? AccountUid { get; set; }
    public string? Iban { get; set; }
    public string? AccountName { get; set; }
    public string? Currency { get; set; }

    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
