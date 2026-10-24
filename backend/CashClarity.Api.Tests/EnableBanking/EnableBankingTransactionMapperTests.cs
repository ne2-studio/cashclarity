using CashClarity.Api.Services.EnableBanking;
using Xunit;

namespace CashClarity.Api.Tests.EnableBanking;

public class EnableBankingTransactionMapperTests
{
    [Fact]
    public void Maps_sign_date_and_concept()
    {
        var movement = Assert.Single(EnableBankingTransactionMapper.Map([
            Transaction(reference: "ref-1", amount: "42.30", indicator: "DBIT", remittance: ["Compra", " Supermercado "]),
        ]));

        Assert.Equal("ref:ref-1", movement.ExternalId);
        Assert.Equal(new DateOnly(2026, 10, 3), movement.Date);
        Assert.Equal(-42.30m, movement.Amount);
        Assert.Equal("Compra Supermercado", movement.Description);
    }

    [Fact]
    public void Falls_back_to_counterparty_when_there_is_no_remittance_information()
    {
        var movements = EnableBankingTransactionMapper.Map([
            Transaction(indicator: "DBIT", remittance: [], creditor: "Iberdrola"),
            Transaction(indicator: "CRDT", remittance: null, debtor: "Cliente Norte"),
            Transaction(indicator: "CRDT", remittance: null),
        ]);

        Assert.Equal(["Iberdrola", "Cliente Norte", "Movimiento bancario"], movements.Select(m => m.Description));
    }

    [Fact]
    public void Uses_transaction_id_then_content_hash_when_there_is_no_entry_reference()
    {
        var movements = EnableBankingTransactionMapper.Map([
            Transaction(transactionId: "tx-9"),
            Transaction(remittance: ["Cafe"]),
        ]);

        Assert.Equal("tx:tx-9", movements[0].ExternalId);
        Assert.StartsWith("hash:", movements[1].ExternalId);
    }

    [Fact]
    public void Content_hash_is_stable_and_identical_transactions_get_distinct_occurrence_keys()
    {
        EnableBankingTransaction Cafe() => Transaction(remittance: ["cafe  bar"]);
        var first = EnableBankingTransactionMapper.Map([Cafe(), Cafe()]);
        var second = EnableBankingTransactionMapper.Map([Transaction(remittance: [" CAFE BAR "]), Cafe()]);

        Assert.Equal(first.Select(m => m.ExternalId), second.Select(m => m.ExternalId));
        Assert.Equal(first[0].ExternalId + "#1", first[1].ExternalId);
    }

    [Fact]
    public void Skips_non_booked_transactions_and_uses_value_date_when_booking_date_is_missing()
    {
        var movements = EnableBankingTransactionMapper.Map([
            Transaction(reference: "pending", status: "PDNG"),
            Transaction(reference: "booked", bookingDate: null, valueDate: "2026-10-05"),
        ]);

        var movement = Assert.Single(movements);
        Assert.Equal("ref:booked", movement.ExternalId);
        Assert.Equal(new DateOnly(2026, 10, 5), movement.Date);
    }

    private static EnableBankingTransaction Transaction(
        string? reference = null,
        string? transactionId = null,
        string amount = "3.50",
        string indicator = "DBIT",
        string? status = "BOOK",
        string? bookingDate = "2026-10-03",
        string? valueDate = null,
        List<string>? remittance = null,
        string? creditor = null,
        string? debtor = null) =>
        new(reference, transactionId, new EnableBankingAmount(amount, "EUR"), indicator, status, bookingDate, valueDate, null,
            remittance, creditor is null ? null : new EnableBankingParty(creditor), debtor is null ? null : new EnableBankingParty(debtor));
}
