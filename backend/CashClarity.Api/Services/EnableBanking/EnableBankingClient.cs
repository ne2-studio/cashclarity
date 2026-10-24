using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CashClarity.Api.Services.EnableBanking;

public interface IEnableBankingClient
{
    bool IsConfigured { get; }
    Task<List<EnableBankingAspsp>> GetAspsps(string country);
    Task<string> StartAuthorization(EnableBankingAspspRef aspsp, string psuType, DateTimeOffset validUntil, string state);
    Task<EnableBankingSession> CreateSession(string code);
    Task<EnableBankingTransactionsPage> GetTransactions(string accountUid, DateOnly dateFrom, DateOnly dateTo, string? continuationKey);
    Task<List<EnableBankingBalance>> GetBalances(string accountUid);
    Task DeleteSession(string sessionId);
}

/// <summary>Fallo al hablar con Enable Banking. Se traduce a 502, o a 400 si el consentimiento ya no es válido.</summary>
public class BankProviderException(string message, bool consentInvalid = false) : Exception(message)
{
    public bool ConsentInvalid { get; } = consentInvalid;
}

public class EnableBankingClient(HttpClient http, IOptions<EnableBankingOptions> options, TimeProvider clock) : IEnableBankingClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly EnableBankingOptions settings = options.Value;
    private readonly Lazy<RSA> privateKey = new(() => LoadPrivateKey(options.Value));

    public bool IsConfigured => settings.IsConfigured;

    public async Task<List<EnableBankingAspsp>> GetAspsps(string country)
    {
        var list = await Send<EnableBankingAspspList>(HttpMethod.Get, $"/aspsps?country={Uri.EscapeDataString(country)}");
        return list.Aspsps;
    }

    public async Task<string> StartAuthorization(EnableBankingAspspRef aspsp, string psuType, DateTimeOffset validUntil, string state)
    {
        var request = new EnableBankingAuthRequest(new EnableBankingAccess(validUntil), aspsp, state, settings.RedirectUrl!, psuType);
        var response = await Send<EnableBankingAuthResponse>(HttpMethod.Post, "/auth", request);
        return response.Url;
    }

    public Task<EnableBankingSession> CreateSession(string code) =>
        Send<EnableBankingSession>(HttpMethod.Post, "/sessions", new EnableBankingSessionRequest(code));

    public Task<EnableBankingTransactionsPage> GetTransactions(string accountUid, DateOnly dateFrom, DateOnly dateTo, string? continuationKey)
    {
        var query = $"date_from={dateFrom:yyyy-MM-dd}&date_to={dateTo:yyyy-MM-dd}&transaction_status=BOOK";
        if (!string.IsNullOrEmpty(continuationKey)) query += $"&continuation_key={Uri.EscapeDataString(continuationKey)}";
        return Send<EnableBankingTransactionsPage>(HttpMethod.Get, $"/accounts/{Uri.EscapeDataString(accountUid)}/transactions?{query}");
    }

    public async Task<List<EnableBankingBalance>> GetBalances(string accountUid)
    {
        var response = await Send<EnableBankingBalances>(HttpMethod.Get, $"/accounts/{Uri.EscapeDataString(accountUid)}/balances");
        return response.Balances;
    }

    public async Task DeleteSession(string sessionId)
    {
        using var request = CreateRequest(HttpMethod.Delete, $"/sessions/{Uri.EscapeDataString(sessionId)}", null);
        using var response = await http.SendAsync(request);
        await EnsureSuccess(response);
    }

    private async Task<T> Send<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = CreateRequest(method, path, body);
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request);
        }
        catch (HttpRequestException e)
        {
            throw new BankProviderException($"No se ha podido contactar con Enable Banking: {e.Message}");
        }

        using (response)
        {
            await EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<T>(Json)
                ?? throw new BankProviderException("Respuesta vacía de Enable Banking");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body)
    {
        if (!IsConfigured) throw new BankProviderException("La integración con Enable Banking no está configurada");

        var request = new HttpRequestMessage(method, settings.BaseUrl.TrimEnd('/') + path);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            EnableBankingJwt.Create(settings.ApplicationId!, privateKey.Value, clock.GetUtcNow()));
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        return request;
    }

    private static readonly HashSet<string> InvalidSessionErrors = ["EXPIRED_SESSION", "CLOSED_SESSION", "REVOKED_SESSION"];

    private static async Task EnsureSuccess(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync();
        string? errorCode = null;
        string? message = null;
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                errorCode = error.GetString();
            if (document.RootElement.TryGetProperty("message", out var text) && text.ValueKind == JsonValueKind.String)
                message = text.GetString();
        }
        catch (JsonException)
        {
        }

        if (errorCode is not null && InvalidSessionErrors.Contains(errorCode))
        {
            throw new BankProviderException(
                "El consentimiento bancario no es válido o ha caducado. Vuelve a vincular la cuenta.",
                consentInvalid: true);
        }

        throw new BankProviderException(
            $"Enable Banking respondió {(int)response.StatusCode}: {errorCode ?? ""} {message ?? ""}".TrimEnd());
    }

    private static RSA LoadPrivateKey(EnableBankingOptions settings)
    {
        var pem = !string.IsNullOrWhiteSpace(settings.PrivateKey)
            ? settings.PrivateKey
            : File.ReadAllText(settings.PrivateKeyPath!);
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);
        return rsa;
    }
}
