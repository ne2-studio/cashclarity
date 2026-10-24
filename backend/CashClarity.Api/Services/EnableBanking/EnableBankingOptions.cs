namespace CashClarity.Api.Services.EnableBanking;

/// <summary>
/// Configuración de Enable Banking. Los secretos llegan solo por variables de entorno
/// (EnableBanking__ApplicationId, EnableBanking__PrivateKeyPath o EnableBanking__PrivateKey)
/// y nunca se versionan.
/// </summary>
public class EnableBankingOptions
{
    public const string SectionName = "EnableBanking";

    public string BaseUrl { get; set; } = "https://api.enablebanking.com";
    public string? ApplicationId { get; set; }

    /// <summary>Ruta a un fichero PEM fuera del repositorio con la clave privada de la aplicación.</summary>
    public string? PrivateKeyPath { get; set; }

    /// <summary>Alternativa a <see cref="PrivateKeyPath"/>: el PEM en sí, para entornos que inyectan secretos como texto.</summary>
    public string? PrivateKey { get; set; }

    /// <summary>URL https del frontend a la que vuelve el banco; debe estar registrada en la aplicación de Enable Banking.</summary>
    public string? RedirectUrl { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApplicationId) &&
        !string.IsNullOrWhiteSpace(RedirectUrl) &&
        (!string.IsNullOrWhiteSpace(PrivateKeyPath) || !string.IsNullOrWhiteSpace(PrivateKey));
}
