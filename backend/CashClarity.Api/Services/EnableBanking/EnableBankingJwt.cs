using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CashClarity.Api.Services.EnableBanking;

/// <summary>
/// JWT RS256 con el que la aplicación se autentica ante la API de Enable Banking.
/// </summary>
public static class EnableBankingJwt
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    public static string Create(string applicationId, RSA privateKey, DateTimeOffset now)
    {
        var header = new Dictionary<string, object> { ["typ"] = "JWT", ["alg"] = "RS256", ["kid"] = applicationId };
        var iat = now.ToUnixTimeSeconds();
        var payload = new Dictionary<string, object>
        {
            ["iss"] = "enablebanking.com",
            ["aud"] = "api.enablebanking.com",
            ["iat"] = iat,
            ["exp"] = iat + (long)Lifetime.TotalSeconds,
        };

        var signingInput = $"{Base64Url(JsonSerializer.SerializeToUtf8Bytes(header))}.{Base64Url(JsonSerializer.SerializeToUtf8Bytes(payload))}";
        var signature = privateKey.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return $"{signingInput}.{Base64Url(signature)}";
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
