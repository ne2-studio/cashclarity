using System.IO.Compression;
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
    public async Task Preview_parses_bank_excel_with_title_rows_and_bank_headers()
    {
        var service = new BankMovementImportService(new InMemoryBankMovementsRepository());

        var preview = await service.Preview(BankExcel(), "user-a");

        Assert.Equal(1, preview.Summary.Valid);
        var row = Assert.Single(preview.Rows);
        Assert.Equal(5, row.RowNumber);
        Assert.Equal("2026-08-01", row.Date);
        Assert.Equal("Nomina Agosto Empresa", row.Description);
        Assert.Equal(1500.25m, row.Amount);
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

    private static MemoryStream BankExcel()
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                </Types>
                """);
            WriteEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                </Relationships>
                """);
            WriteEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="Movimientos" sheetId="1" r:id="rId1"/></sheets>
                </workbook>
                """);
            WriteEntry(archive, "xl/worksheets/sheet1.xml", """
                <?xml version="1.0" encoding="UTF-8"?>
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <sheetData>
                    <row r="1"><c r="A1" t="inlineStr"><is><t>Movimientos de la cuenta</t></is></c></row>
                    <row r="2"><c r="A2" t="inlineStr"><is><t>Importes expresados en euros</t></is></c></row>
                    <row r="3"></row>
                    <row r="4">
                      <c r="A4" t="inlineStr"><is><t>Fecha</t></is></c>
                      <c r="B4" t="inlineStr"><is><t>Fecha valor</t></is></c>
                      <c r="C4" t="inlineStr"><is><t>Movimiento</t></is></c>
                      <c r="D4" t="inlineStr"><is><t>Más datos</t></is></c>
                      <c r="E4" t="inlineStr"><is><t>Importe</t></is></c>
                      <c r="F4" t="inlineStr"><is><t>Saldo</t></is></c>
                    </row>
                    <row r="5">
                      <c r="A5" t="inlineStr"><is><t>01/08/2026</t></is></c>
                      <c r="B5" t="inlineStr"><is><t>01/08/2026</t></is></c>
                      <c r="C5" t="inlineStr"><is><t>Nomina Agosto</t></is></c>
                      <c r="D5" t="inlineStr"><is><t>Empresa</t></is></c>
                      <c r="E5"><v>1500.25</v></c>
                      <c r="F5"><v>1500.25</v></c>
                    </row>
                  </sheetData>
                </worksheet>
                """);
        }

        stream.Position = 0;
        return stream;
    }

    private static void WriteEntry(ZipArchive archive, string path, string contents)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(contents);
    }
}
