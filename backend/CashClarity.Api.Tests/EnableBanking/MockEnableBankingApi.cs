using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CashClarity.Api.Services.EnableBanking;
using Microsoft.Extensions.Options;

namespace CashClarity.Api.Tests.EnableBanking;

/// <summary>
/// Simula la API HTTP de Enable Banking en memoria y registra las peticiones recibidas.
/// </summary>
public class MockEnableBankingApi : HttpMessageHandler
{
    public const string ApplicationId = "app-123";
    public const string RedirectUrl = "https://cashclarity.test/bank-link/callback";

    public RSA Key { get; } = RSA.Create(2048);
    public List<RecordedRequest> Requests { get; } = [];
    public List<JsonObject> Accounts { get; set; } = [Account("uid-1", "ES91 2100 0418 4502 0005 1332")];
    public List<JsonObject> Transactions { get; set; } = [];
    public int PageSize { get; set; } = 100;
    public DateTimeOffset SessionValidUntil { get; set; } = new(2027, 1, 20, 0, 0, 0, TimeSpan.Zero);
    public string? TransactionsError { get; set; }
    public int SessionCounter { get; private set; }

    public EnableBankingClient CreateClient(TimeProvider clock) =>
        new(new HttpClient(this), Options.Create(new EnableBankingOptions
        {
            ApplicationId = ApplicationId,
            PrivateKey = Key.ExportPkcs8PrivateKeyPem(),
            RedirectUrl = RedirectUrl,
            BaseUrl = "https://api.enablebanking.test",
        }), clock);

    public static JsonObject Account(string uid, string iban) => new()
    {
        ["uid"] = uid,
        ["account_id"] = new JsonObject { ["iban"] = iban },
        ["name"] = "Cuenta " + uid,
        ["currency"] = "EUR",
    };

    public static JsonObject Transaction(
        string? entryReference, string bookingDate, string amount, string indicator, string concept, string status = "BOOK") => new()
    {
        ["entry_reference"] = entryReference,
        ["transaction_amount"] = new JsonObject { ["amount"] = amount, ["currency"] = "EUR" },
        ["credit_debit_indicator"] = indicator,
        ["status"] = status,
        ["booking_date"] = bookingDate,
        ["remittance_information"] = new JsonArray(concept),
    };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var recorded = new RecordedRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.Parameter, body);
        Requests.Add(recorded);

        if (!IsValidJwt(recorded.Jwt)) return Json(HttpStatusCode.Unauthorized, new JsonObject { ["error"] = "UNAUTHORIZED_ACCESS", ["message"] = "bad jwt" });

        var path = request.RequestUri!.AbsolutePath;
        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);

        return (request.Method.Method, path) switch
        {
            ("GET", "/aspsps") => Json(HttpStatusCode.OK, new JsonObject
            {
                ["aspsps"] = new JsonArray(
                    new JsonObject { ["name"] = "CaixaBank", ["country"] = "ES", ["psu_types"] = new JsonArray("personal", "business"), ["maximum_consent_validity"] = 7776000 },
                    new JsonObject { ["name"] = "N26", ["country"] = "ES", ["psu_types"] = new JsonArray("personal"), ["maximum_consent_validity"] = 15552000 }),
            }),
            ("POST", "/auth") => Json(HttpStatusCode.OK, new JsonObject
            {
                ["url"] = "https://bank.example/login?state=" + JsonNode.Parse(body!)!["state"]!.GetValue<string>(),
                ["authorization_id"] = "auth-1",
            }),
            ("POST", "/sessions") => Json(HttpStatusCode.OK, new JsonObject
            {
                ["session_id"] = $"session-{++SessionCounter}",
                ["accounts"] = new JsonArray(Accounts.Select(a => (JsonNode)a.DeepClone()).ToArray()),
                ["access"] = new JsonObject { ["valid_until"] = SessionValidUntil.ToString("O") },
            }),
            ("DELETE", var p) when p.StartsWith("/sessions/") => Json(HttpStatusCode.OK, new JsonObject()),
            ("GET", var p) when p.EndsWith("/transactions") => TransactionsPage(query),
            ("GET", var p) when p.EndsWith("/balances") => Json(HttpStatusCode.OK, new JsonObject
            {
                ["balances"] = new JsonArray(new JsonObject
                {
                    ["name"] = "Saldo",
                    ["balance_amount"] = new JsonObject { ["amount"] = "1234.56", ["currency"] = "EUR" },
                    ["balance_type"] = "CLBD",
                }),
            }),
            _ => Json(HttpStatusCode.NotFound, new JsonObject { ["message"] = "not found" }),
        };
    }

    private HttpResponseMessage TransactionsPage(System.Collections.Specialized.NameValueCollection query)
    {
        if (TransactionsError is not null)
            return Json(HttpStatusCode.UnprocessableEntity, new JsonObject { ["error"] = TransactionsError, ["message"] = "session problem" });

        var from = query["date_from"]!;
        var to = query["date_to"]!;
        var inRange = Transactions
            .Where(t => string.CompareOrdinal(t["booking_date"]!.GetValue<string>(), from) >= 0 &&
                        string.CompareOrdinal(t["booking_date"]!.GetValue<string>(), to) <= 0)
            .ToList();
        var offset = int.Parse(query["continuation_key"] ?? "0");
        var page = inRange.Skip(offset).Take(PageSize).Select(t => (JsonNode)t.DeepClone()).ToArray();
        var next = offset + PageSize < inRange.Count ? (offset + PageSize).ToString() : null;
        return Json(HttpStatusCode.OK, new JsonObject { ["transactions"] = new JsonArray(page), ["continuation_key"] = next });
    }

    private bool IsValidJwt(string? jwt)
    {
        if (jwt is null) return false;
        var parts = jwt.Split('.');
        if (parts.Length != 3) return false;
        var header = JsonNode.Parse(Base64UrlDecode(parts[0]))!;
        var payload = JsonNode.Parse(Base64UrlDecode(parts[1]))!;
        return header["alg"]!.GetValue<string>() == "RS256" &&
               header["kid"]!.GetValue<string>() == ApplicationId &&
               payload["iss"]!.GetValue<string>() == "enablebanking.com" &&
               payload["aud"]!.GetValue<string>() == "api.enablebanking.com" &&
               payload["exp"]!.GetValue<long>() - payload["iat"]!.GetValue<long>() <= 3600 &&
               Key.VerifyData(Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"), Base64UrlDecode(parts[2]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    public static byte[] Base64UrlDecode(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }

    private static HttpResponseMessage Json(HttpStatusCode status, JsonNode body) =>
        new(status) { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };

    public record RecordedRequest(HttpMethod Method, Uri Uri, string? Jwt, string? Body)
    {
        public JsonNode? JsonBody => Body is null ? null : JsonNode.Parse(Body);
    }
}

public class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}
