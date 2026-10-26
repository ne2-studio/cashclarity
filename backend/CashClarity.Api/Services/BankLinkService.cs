using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CashClarity.Api.Contracts;
using CashClarity.Api.Domain;
using CashClarity.Api.Repositories;
using CashClarity.Api.Services.EnableBanking;

namespace CashClarity.Api.Services;

public interface IBankLinkService
{
    Task<BankLinkResponse> GetBankLink(string userId, Guid workspaceId);
    Task<List<BankAspspResponse>> GetAspsps(string country);
    Task<BankAuthorizationStartResponse> StartAuthorization(BankAuthorizationStartRequest request, string userId, Guid workspaceId);
    Task<BankLinkResponse> CompleteAuthorization(BankAuthorizationCompleteRequest request, string userId, Guid workspaceId);
    Task<BankLinkResponse> SelectAccount(BankAccountSelectRequest request, string userId, Guid workspaceId);
    Task<BankSyncResponse> Sync(string userId, Guid workspaceId);
    Task Unlink(string userId, Guid workspaceId);
}

/// <summary>
/// Vincula un workspace con una cuenta bancaria real (Enable Banking) y sincroniza sus
/// movimientos contabilizados de forma idempotente.
/// </summary>
public class BankLinkService(
    IBankLinksRepository bankLinks,
    IBankMovementsRepository bankMovements,
    IEnableBankingClient enableBanking,
    TimeProvider clock,
    ILogger<BankLinkService> logger) : IBankLinkService
{
    /// <summary>PSD2 limita el acceso a cuentas a 90 días por consentimiento.</summary>
    public static readonly TimeSpan MaxConsentValidity = TimeSpan.FromDays(90);

    /// <summary>Los bancos contabilizan con retraso; cada sync vuelve a pedir estos días previos a la última.</summary>
    public const int SyncOverlapDays = 7;

    private static readonly TimeSpan PendingAuthorizationLifetime = TimeSpan.FromMinutes(30);
    private static readonly string[] PsuTypes = ["personal", "business"];
    private static readonly string[] PreferredBalanceTypes = ["CLBD", "ITBD", "XPCD", "CLAV", "ITAV", "OPBD"];

    public async Task<BankLinkResponse> GetBankLink(string userId, Guid workspaceId) =>
        ToResponse(await bankLinks.GetBankLink(workspaceId));

    public async Task<List<BankAspspResponse>> GetAspsps(string country)
    {
        EnsureConfigured();
        var aspsps = await enableBanking.GetAspsps(country.ToUpperInvariant());
        return aspsps
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(a => new BankAspspResponse(
                a.Name,
                a.Country,
                a.PsuTypes ?? [],
                a.MaximumConsentValidity is { } seconds ? (int)TimeSpan.FromSeconds(seconds).TotalDays : null,
                a.Logo))
            .ToList();
    }

    public async Task<BankAuthorizationStartResponse> StartAuthorization(BankAuthorizationStartRequest request, string userId, Guid workspaceId)
    {
        EnsureConfigured();
        if (!PsuTypes.Contains(request.PsuType))
            throw new InvalidOperationException("Tipo de cliente no válido: debe ser personal o business");
        if (string.IsNullOrWhiteSpace(request.AspspName) || string.IsNullOrWhiteSpace(request.AspspCountry))
            throw new InvalidOperationException("Banco obligatorio");

        var country = request.AspspCountry.ToUpperInvariant();
        var aspsp = (await enableBanking.GetAspsps(country))
            .FirstOrDefault(a => a.Name == request.AspspName)
            ?? throw new InvalidOperationException($"Banco no disponible en Enable Banking: {request.AspspName}");
        if (aspsp.PsuTypes is { Count: > 0 } supported && !supported.Contains(request.PsuType))
            throw new InvalidOperationException($"{aspsp.Name} no admite clientes de tipo {request.PsuType}");

        var link = await bankLinks.GetBankLink(workspaceId);
        if (link?.Iban is not null && link.AspspName != aspsp.Name)
            throw new InvalidOperationException(
                $"Este workspace ya está vinculado a {link.AspspName} ({link.Iban}). Desvincúlalo antes de vincular otro banco.");

        var now = clock.GetUtcNow();
        var consentValidity = aspsp.MaximumConsentValidity is { } seconds && TimeSpan.FromSeconds(seconds) < MaxConsentValidity
            ? TimeSpan.FromSeconds(seconds)
            : MaxConsentValidity;
        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var isNew = link is null;
        link ??= new BankLink
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            UserId = userId,
            SyncFrom = request.SyncFrom is null ? await DefaultSyncFrom(userId, workspaceId) : ParseDate(request.SyncFrom),
            CreatedAt = now.UtcDateTime,
        };
        if (!isNew && request.SyncFrom is not null && link.LastSyncedAt is null) link.SyncFrom = ParseDate(request.SyncFrom);
        link.AspspName = aspsp.Name;
        link.AspspCountry = aspsp.Country;
        link.PsuType = request.PsuType;
        link.PendingState = state;
        link.PendingStateExpiresAt = (now + PendingAuthorizationLifetime).UtcDateTime;
        link.UpdatedAt = now.UtcDateTime;

        // Se pide la URL antes de guardar: si el banco rechaza la petición no queda un vínculo a medias.
        var url = await enableBanking.StartAuthorization(
            new EnableBankingAspspRef(aspsp.Name, aspsp.Country),
            request.PsuType,
            now + consentValidity,
            state);

        if (isNew) await bankLinks.AddBankLink(link);
        else await bankLinks.UpdateBankLink(link);

        return new BankAuthorizationStartResponse(url);
    }

    public async Task<BankLinkResponse> CompleteAuthorization(BankAuthorizationCompleteRequest request, string userId, Guid workspaceId)
    {
        EnsureConfigured();
        var link = await bankLinks.GetBankLink(workspaceId);
        if (link?.PendingState is null ||
            link.PendingStateExpiresAt < clock.GetUtcNow().UtcDateTime ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(link.PendingState), Encoding.UTF8.GetBytes(request.State ?? "")))
        {
            throw new InvalidOperationException("La autorización bancaria no es válida o ha caducado. Vuelve a iniciar la vinculación.");
        }
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new InvalidOperationException("Falta el código de autorización del banco");

        var session = await enableBanking.CreateSession(request.Code);
        var options = session.Accounts
            .Select(a => new BankAccountOption(a.Uid, NormalizeIban(a.AccountId?.Iban), a.Name, a.Currency))
            .ToList();

        // Re-vinculación: los uid cambian con cada sesión, el IBAN no.
        var selected = link.Iban is not null
            ? options.FirstOrDefault(o => o.Iban == link.Iban)
            : options.Count == 1 ? options[0] : null;
        if ((link.Iban is not null && selected is null) || options.Count == 0)
        {
            await TryDeleteSession(session.SessionId);
            throw new InvalidOperationException(link.Iban is not null
                ? $"La nueva autorización no incluye la cuenta vinculada ({link.Iban}). Autoriza esa cuenta en el banco."
                : "El banco no ha devuelto ninguna cuenta autorizada");
        }

        var previousSessionId = link.SessionId;
        link.PendingState = null;
        link.PendingStateExpiresAt = null;
        link.SessionId = session.SessionId;
        link.ValidUntil = session.Access.ValidUntil.UtcDateTime;

        if (selected is not null)
        {
            ApplyAccount(link, selected);
        }
        else
        {
            link.AccountUid = null;
            link.AccountOptionsJson = JsonSerializer.Serialize(options);
        }

        await bankLinks.UpdateBankLink(link);
        if (previousSessionId is not null && previousSessionId != link.SessionId) await TryDeleteSession(previousSessionId);
        return ToResponse(link);
    }

    public async Task<BankLinkResponse> SelectAccount(BankAccountSelectRequest request, string userId, Guid workspaceId)
    {
        var link = await bankLinks.GetBankLink(workspaceId) ?? throw new NotFoundException("No hay ninguna cuenta bancaria vinculada");
        var option = ReadOptions(link).FirstOrDefault(o => o.Uid == request.AccountUid)
            ?? throw new InvalidOperationException("Cuenta no disponible en la autorización actual");

        ApplyAccount(link, option);
        await bankLinks.UpdateBankLink(link);
        return ToResponse(link);
    }

    public async Task<BankSyncResponse> Sync(string userId, Guid workspaceId)
    {
        EnsureConfigured();
        var link = await bankLinks.GetBankLink(workspaceId) ?? throw new NotFoundException("No hay ninguna cuenta bancaria vinculada");
        var now = clock.GetUtcNow();
        var status = StatusOf(link);
        if (status == BankLinkStatus.Expired)
            throw new InvalidOperationException("El consentimiento bancario ha caducado. Vuelve a vincular la cuenta.");
        if (status != BankLinkStatus.Active)
            throw new InvalidOperationException("La vinculación bancaria no está completa");

        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var dateFrom = link.LastSyncedAt is { } last
            ? Max(link.SyncFrom, DateOnly.FromDateTime(last).AddDays(-SyncOverlapDays))
            : link.SyncFrom;

        var transactions = new List<EnableBankingTransaction>();
        string? continuationKey = null;
        do
        {
            var page = await enableBanking.GetTransactions(link.AccountUid!, dateFrom, today, continuationKey);
            transactions.AddRange(page.Transactions);
            continuationKey = page.ContinuationKey;
        } while (!string.IsNullOrEmpty(continuationKey));

        var mapped = EnableBankingTransactionMapper.Map(transactions);
        var existing = await bankMovements.GetBankMovements(userId, workspaceId);
        var created = await bankMovements.AddSyncedBankMovements(mapped, userId, workspaceId);

        link.LastSyncedAt = now.UtcDateTime;
        await bankLinks.UpdateBankLink(link);

        return new BankSyncResponse(
            created,
            mapped.Count - created.Count,
            FindPossibleDuplicates(created, existing),
            dateFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            await TryGetBalance(link.AccountUid!),
            ToResponse(link));
    }

    public async Task Unlink(string userId, Guid workspaceId)
    {
        var link = await bankLinks.GetBankLink(workspaceId);
        if (link is null) return;
        if (link.SessionId is not null && enableBanking.IsConfigured) await TryDeleteSession(link.SessionId);
        await bankLinks.DeleteBankLink(workspaceId);
    }

    /// <summary>
    /// Movimientos recién sincronizados que coinciden en fecha e importe con uno manual o de CSV:
    /// probablemente el mismo movimiento con otro concepto. Se avisa, no se bloquea.
    /// </summary>
    private static List<BankPossibleDuplicate> FindPossibleDuplicates(
        List<BankMovementResponse> created,
        List<BankMovementResponse> existing)
    {
        var unsynced = existing
            .Where(m => m.ExternalId is null)
            .GroupBy(m => (m.Date.Date, m.Amount))
            .ToDictionary(g => g.Key, g => g.First().Id);

        return created
            .Where(m => unsynced.ContainsKey((m.Date.Date, m.Amount)))
            .Select(m => new BankPossibleDuplicate(m.Id, unsynced[(m.Date.Date, m.Amount)]))
            .ToList();
    }

    private async Task<BankBalanceResponse?> TryGetBalance(string accountUid)
    {
        try
        {
            var balances = await enableBanking.GetBalances(accountUid);
            var balance = PreferredBalanceTypes
                .Select(type => balances.FirstOrDefault(b => b.BalanceType == type))
                .FirstOrDefault(b => b is not null) ?? balances.FirstOrDefault();
            return balance is null
                ? null
                : new BankBalanceResponse(
                    decimal.Parse(balance.BalanceAmount.Amount, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture),
                    balance.BalanceAmount.Currency,
                    balance.BalanceType,
                    balance.ReferenceDate);
        }
        catch (Exception e) when (e is BankProviderException or FormatException)
        {
            logger.LogWarning(e, "No se ha podido leer el saldo bancario");
            return null;
        }
    }

    private async Task TryDeleteSession(string sessionId)
    {
        try
        {
            await enableBanking.DeleteSession(sessionId);
        }
        catch (BankProviderException e)
        {
            logger.LogWarning(e, "No se ha podido cerrar la sesión de Enable Banking");
        }
    }

    private async Task<DateOnly> DefaultSyncFrom(string userId, Guid workspaceId)
    {
        var latest = (await bankMovements.GetBankMovements(userId, workspaceId)).MaxBy(m => m.Date);
        return latest is null
            ? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(-90)
            : DateOnly.FromDateTime(latest.Date).AddDays(1);
    }

    private void EnsureConfigured()
    {
        if (!enableBanking.IsConfigured)
            throw new InvalidOperationException("La integración con Enable Banking no está configurada en este entorno");
    }

    private string StatusOf(BankLink? link)
    {
        if (link is null) return BankLinkStatus.None;
        if (link.SessionId is null) return BankLinkStatus.Pending;
        if (link.ValidUntil <= clock.GetUtcNow().UtcDateTime) return BankLinkStatus.Expired;
        return link.AccountUid is null ? BankLinkStatus.AwaitingAccount : BankLinkStatus.Active;
    }

    private BankLinkResponse ToResponse(BankLink? link)
    {
        var status = StatusOf(link);
        if (link is null) return new BankLinkResponse(enableBanking.IsConfigured, status);
        return new BankLinkResponse(
            enableBanking.IsConfigured,
            status,
            link.AspspName,
            link.AspspCountry,
            link.PsuType,
            link.Iban,
            link.AccountName,
            link.Currency,
            link.ValidUntil,
            link.SyncFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            link.LastSyncedAt,
            status == BankLinkStatus.AwaitingAccount ? ReadOptions(link) : null);
    }

    private static void ApplyAccount(BankLink link, BankAccountOption option)
    {
        link.AccountUid = option.Uid;
        link.Iban = option.Iban;
        link.AccountName = option.Name;
        link.Currency = option.Currency;
        link.AccountOptionsJson = null;
    }

    private static List<BankAccountOption> ReadOptions(BankLink link) =>
        link.AccountOptionsJson is null ? [] : JsonSerializer.Deserialize<List<BankAccountOption>>(link.AccountOptionsJson) ?? [];

    private static string? NormalizeIban(string? iban) =>
        string.IsNullOrWhiteSpace(iban) ? null : iban.Replace(" ", "").ToUpperInvariant();

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;

    private static DateOnly ParseDate(string value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new InvalidOperationException("Fecha inicial no válida (yyyy-MM-dd)");
}
