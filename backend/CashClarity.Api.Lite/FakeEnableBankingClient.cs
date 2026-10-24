using CashClarity.Api.Services.EnableBanking;

namespace CashClarity.Api.Lite;

/// <summary>
/// Enable Banking simulado para frontend-dev y la aceptación visual: el "login del banco" vuelve
/// directamente al callback del frontend y siempre devuelve la misma cuenta y movimientos.
/// </summary>
public class FakeEnableBankingClient(TimeProvider clock) : IEnableBankingClient
{
    public const string FakeCode = "fake-code";

    public bool IsConfigured => true;

    public Task<List<EnableBankingAspsp>> GetAspsps(string country) => Task.FromResult<List<EnableBankingAspsp>>(
    [
        new("CaixaBank", "ES", ["personal", "business"], 90 * 24 * 3600, null),
        new("MyInvestor", "ES", ["personal"], 90 * 24 * 3600, null),
        new("N26", "ES", ["personal"], 90 * 24 * 3600, null),
    ]);

    public Task<string> StartAuthorization(EnableBankingAspspRef aspsp, string psuType, DateTimeOffset validUntil, string state) =>
        Task.FromResult($"/bank-link/callback?code={FakeCode}&state={Uri.EscapeDataString(state)}");

    public Task<EnableBankingSession> CreateSession(string code)
    {
        if (code != FakeCode) throw new BankProviderException("Código de autorización desconocido");
        return Task.FromResult(new EnableBankingSession(
            $"fake-session-{Guid.NewGuid():N}",
            [new EnableBankingAccount("fake-account", new EnableBankingAccountId("ES9121000418450200051332"), "Cuenta corriente", "EUR")],
            new EnableBankingAccess(clock.GetUtcNow().AddDays(90))));
    }

    public Task<EnableBankingTransactionsPage> GetTransactions(string accountUid, DateOnly dateFrom, DateOnly dateTo, string? continuationKey)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        List<EnableBankingTransaction> all =
        [
            Transaction("fake-1", today.AddDays(-3), "1500.00", "CRDT", "Nomina empresa"),
            Transaction("fake-2", today.AddDays(-2), "42.30", "DBIT", "Supermercado"),
            Transaction("fake-3", today.AddDays(-1), "9.99", "DBIT", "Suscripcion musica"),
        ];
        return Task.FromResult(new EnableBankingTransactionsPage(
            all.Where(t => DateOnly.Parse(t.BookingDate!) >= dateFrom && DateOnly.Parse(t.BookingDate!) <= dateTo).ToList(),
            null));
    }

    public Task<List<EnableBankingBalance>> GetBalances(string accountUid) => Task.FromResult<List<EnableBankingBalance>>(
        [new("Saldo", new EnableBankingAmount("1447.71", "EUR"), "CLBD", null)]);

    public Task DeleteSession(string sessionId) => Task.CompletedTask;

    private static EnableBankingTransaction Transaction(string reference, DateOnly date, string amount, string indicator, string concept) =>
        new(reference, null, new EnableBankingAmount(amount, "EUR"), indicator, "BOOK",
            date.ToString("yyyy-MM-dd"), null, null, [concept], null, null);
}
