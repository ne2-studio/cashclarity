using System.Text;
using CashClarity.Api.Controllers;
using CashClarity.Api.Repositories;
using CashClarity.Api.Services;
using Xunit;

namespace CashClarity.Api.Tests;

public class BankMovementImportServiceTests
{
    [Fact]
    public async Task Preview_parses_semicolon_csv_with_decimal_commas()
    {
        var service = new BankMovementImportService(new InMemoryBankMovementsRepository());

        var preview = await service.Preview(Csv("\"Fecha\";\"Concepto\";\"Cantidad\"\r\n02/01/2026;\"Factura luz\";\"123,45\""), "user-a");

        Assert.Equal(1, preview.Summary.Valid);
        var row = Assert.Single(preview.Rows);
        Assert.Equal("2026-01-02", row.Date);
        Assert.Equal("Factura luz", row.Description);
        Assert.Equal(123.45m, row.Amount);
        Assert.Equal(BankMovementImportRowStatus.Valid, row.Status);
    }

    [Fact]
    public async Task Preview_marks_existing_and_in_file_duplicates()
    {
        var existing = new List<BankMovementResponse>
        {
            new("movement-1", new DateTime(2026, 1, 2), "Factura luz", 123.45m, false, null, null, "user-a"),
        };
        var service = new BankMovementImportService(new InMemoryBankMovementsRepository(existing));

        var preview = await service.Preview(Csv("""
            fecha;Concepto;cantidad
            2026-01-02; factura   luz ;123,45
            2026-01-03;Nomina;1000
            2026-01-03;Nomina;1000,00
            """), "user-a");

        Assert.Equal(2, preview.Summary.Duplicates);
        Assert.Equal("movement-1", preview.Rows[0].DuplicateOfBankMovementId);
        Assert.Equal(BankMovementImportRowStatus.Valid, preview.Rows[1].Status);
        Assert.Equal(BankMovementImportRowStatus.Duplicate, preview.Rows[2].Status);
    }

    [Fact]
    public async Task Commit_inserts_selected_rows_in_batch_and_skips_duplicates()
    {
        var repo = new InMemoryBankMovementsRepository([
            new BankMovementResponse("movement-1", new DateTime(2026, 1, 2), "Factura luz", 123.45m, false, null, null, "user-a"),
        ]);
        var service = new BankMovementImportService(repo);
        var request = new BankMovementImportCommitRequest([
            new BankMovementImportRow(2, "2026-01-02", "Factura luz", 123.45m, "valid", null, [], null),
            new BankMovementImportRow(3, "2026-01-03", "Nomina", 1000m, "valid", null, [], null),
            new BankMovementImportRow(4, null, "Sin fecha", 10m, "invalid", null, ["Fecha invalida"], null),
        ]);

        var result = await service.Commit(request, "user-a");

        Assert.Single(result.Created);
        Assert.Equal(1, result.SkippedDuplicates);
        Assert.Single(result.Failed);
        Assert.Equal(2, (await repo.GetBankMovements("user-a")).Count);
    }

    private static MemoryStream Csv(string text) => new(Encoding.UTF8.GetBytes(text));
}
