using System.Globalization;
using System.Text;
using ExcelDataReader;
using CashClarity.Api.Controllers;
using CashClarity.Api.Repositories;

namespace CashClarity.Api.Services;

public interface IBankMovementImportService
{
    Task<BankMovementImportPreviewResponse> Preview(Stream csv, string userId);
    Task<BankMovementImportCommitResponse> Commit(BankMovementImportCommitRequest request, string userId);
}

public class BankMovementImportService(IBankMovementsRepository bankMovements) : IBankMovementImportService
{
    static BankMovementImportService()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<BankMovementImportPreviewResponse> Preview(Stream importFile, string userId)
    {
        using var memory = new MemoryStream();
        await importFile.CopyToAsync(memory);
        var parsedRows = Parse(memory.ToArray());
        var existing = await bankMovements.GetBankMovements(userId);
        var rows = MarkDuplicates(parsedRows, existing);

        return new BankMovementImportPreviewResponse(rows, BuildSummary(rows));
    }

    public async Task<BankMovementImportCommitResponse> Commit(BankMovementImportCommitRequest request, string userId)
    {
        var rowsToImport = request.Rows.Where(r => r.Selected).ToList();
        var existing = await bankMovements.GetBankMovements(userId);
        var checkedRows = MarkDuplicates(rowsToImport, existing);

        var failed = checkedRows
            .Where(r => r.Status == BankMovementImportRowStatus.Invalid)
            .Select(r => new BankMovementImportFailure(r.RowNumber, string.Join("; ", r.Errors)))
            .ToList();

        var duplicateRows = checkedRows.Where(r => r.Status == BankMovementImportRowStatus.Duplicate).ToList();
        var importableRows = checkedRows
            .Where(r => r.Status == BankMovementImportRowStatus.Valid || r.Status == BankMovementImportRowStatus.Warning)
            .Concat(request.DuplicatePolicy == BankMovementDuplicatePolicy.ImportAnyway ? duplicateRows : [])
            .ToList();

        var created = importableRows.Count == 0
            ? []
            : await bankMovements.AddBankMovements(
                importableRows.Select(r => new BankMovementCreateRequest(r.Date!, r.Description!, r.Amount!.Value)).ToList(),
                userId);

        return new BankMovementImportCommitResponse(
            created,
            request.DuplicatePolicy == BankMovementDuplicatePolicy.Skip ? duplicateRows.Count : 0,
            failed);
    }

    private static List<BankMovementImportRow> Parse(byte[] file)
    {
        return IsExcel(file)
            ? ParseTabular(ReadExcelRecords(file))
            : ParseTabular(ReadCsvRecords(Encoding.UTF8.GetString(file)));
    }

    private static bool IsExcel(byte[] file) =>
        file.Length >= 4 &&
        ((file[0] == 0xD0 && file[1] == 0xCF && file[2] == 0x11 && file[3] == 0xE0) ||
         (file[0] == 0x50 && file[1] == 0x4B && file[2] == 0x03 && file[3] == 0x04));

    private static List<BankMovementImportRow> ParseTabular(List<List<ImportCell>> records)
    {
        var headerRowIndex = FindHeaderRow(records);
        if (headerRowIndex < 0)
        {
            return [InvalidFileRow("Formato de archivo invalido. Debe contener columnas de fecha, concepto/movimiento e importe/cantidad.")];
        }

        var headers = records[headerRowIndex].Select(CleanHeader).ToList();
        var dateIndex = headers.IndexOf("fecha");
        var descriptionIndex = FirstHeaderIndex(headers, "concepto", "movimiento");
        var detailsIndex = FirstHeaderIndex(headers, "mas datos", "más datos");
        var amountIndex = FirstHeaderIndex(headers, "cantidad", "importe");

        if (dateIndex < 0 || descriptionIndex < 0 || amountIndex < 0)
        {
            return [new BankMovementImportRow(
                1,
                null,
                null,
                null,
                BankMovementImportRowStatus.Invalid,
                null,
                ["Formato CSV invalido. Debe contener las columnas: fecha; Concepto; cantidad"],
                headers)];
        }

        var rows = new List<BankMovementImportRow>();
        for (var i = headerRowIndex + 1; i < records.Count; i++)
        {
            var record = records[i];
            if (record.All(cell => string.IsNullOrWhiteSpace(CleanCell(cell)))) continue;

            var dateCell = GetCell(record, dateIndex);
            var description = CleanCell(GetCell(record, descriptionIndex));
            var details = detailsIndex < 0 ? string.Empty : CleanCell(GetCell(record, detailsIndex));
            var amountCell = GetCell(record, amountIndex);
            var errors = new List<string>();

            var date = TryParseDate(dateCell, out var parsedDate)
                ? parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : null;
            if (date is null) errors.Add("Fecha invalida");
            if (string.IsNullOrWhiteSpace(description)) errors.Add("Concepto obligatorio");
            var amount = TryParseAmount(amountCell, out var parsedAmount) ? parsedAmount : (decimal?)null;
            if (amount is null) errors.Add("Cantidad invalida");

            var fullDescription = string.IsNullOrWhiteSpace(details)
                ? description
                : $"{description} {details}";

            rows.Add(new BankMovementImportRow(
                i + 1,
                date,
                string.IsNullOrWhiteSpace(fullDescription) ? null : fullDescription,
                amount,
                errors.Count == 0 ? BankMovementImportRowStatus.Valid : BankMovementImportRowStatus.Invalid,
                null,
                errors,
                null));
        }

        return rows.Count == 0
            ? [InvalidFileRow("No se han podido extraer movimientos validos del archivo.")]
            : rows;
    }

    private static BankMovementImportRow InvalidFileRow(string error) =>
        new(1, null, null, null, BankMovementImportRowStatus.Invalid, null, [error], null);

    private static List<BankMovementImportRow> MarkDuplicates(
        List<BankMovementImportRow> rows,
        List<BankMovementResponse> existing)
    {
        var seen = new HashSet<string>();
        var existingByFingerprint = existing
            .GroupBy(Fingerprint)
            .ToDictionary(g => g.Key, g => g.First().Id);

        return rows.Select(row =>
        {
            if (row.Status == BankMovementImportRowStatus.Invalid ||
                row.Date is null ||
                row.Description is null ||
                row.Amount is null)
            {
                return row;
            }

            var fingerprint = Fingerprint(row.Date, row.Description, row.Amount.Value);
            if (existingByFingerprint.TryGetValue(fingerprint, out var duplicateId))
            {
                return row with
                {
                    Status = BankMovementImportRowStatus.Duplicate,
                    DuplicateOfBankMovementId = duplicateId,
                };
            }

            if (!seen.Add(fingerprint))
            {
                return row with { Status = BankMovementImportRowStatus.Duplicate };
            }

            return row;
        }).ToList();
    }

    private static BankMovementImportSummary BuildSummary(List<BankMovementImportRow> rows) =>
        new(
            rows.Count,
            rows.Count(r => r.Status == BankMovementImportRowStatus.Valid),
            rows.Count(r => r.Status == BankMovementImportRowStatus.Duplicate),
            rows.Count(r => r.Status == BankMovementImportRowStatus.Invalid),
            rows.Count(r => r.Status == BankMovementImportRowStatus.Warning));

    private static string Fingerprint(BankMovementResponse movement) =>
        Fingerprint(
            movement.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            movement.Description,
            movement.Amount);

    private static string Fingerprint(string date, string description, decimal amount) =>
        $"{date}|{NormalizeDescription(description)}|{amount:0.00}";

    private static string NormalizeDescription(string description) =>
        string.Join(' ', description.Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static int FindHeaderRow(List<List<ImportCell>> records)
    {
        for (var i = 0; i < records.Count; i++)
        {
            var headers = records[i].Select(CleanHeader).ToList();
            if (headers.Contains("fecha") &&
                FirstHeaderIndex(headers, "concepto", "movimiento") >= 0 &&
                FirstHeaderIndex(headers, "cantidad", "importe") >= 0)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FirstHeaderIndex(List<string> headers, params string[] names)
    {
        foreach (var name in names)
        {
            var index = headers.IndexOf(name);
            if (index >= 0) return index;
        }

        return -1;
    }

    private static string CleanHeader(ImportCell value) => CleanCell(value).ToLowerInvariant();

    private static ImportCell GetCell(List<ImportCell> record, int index) =>
        index < record.Count ? record[index] : new ImportCell(null);

    private static string CleanCell(ImportCell value) => value.Value switch
    {
        null => string.Empty,
        DateTime date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        double number => number.ToString(CultureInfo.InvariantCulture),
        float number => number.ToString(CultureInfo.InvariantCulture),
        decimal number => number.ToString(CultureInfo.InvariantCulture),
        int number => number.ToString(CultureInfo.InvariantCulture),
        long number => number.ToString(CultureInfo.InvariantCulture),
        _ => value.Value.ToString()?.Trim().Trim('"', '\'').Trim() ?? string.Empty,
    };

    private static bool TryParseDate(ImportCell value, out DateTime date)
    {
        if (value.Value is DateTime dateValue)
        {
            date = dateValue.Date;
            return true;
        }

        if (value.Value is double serialDate && serialDate > 20000 && serialDate < 60000)
        {
            date = DateTime.FromOADate(serialDate).Date;
            return true;
        }

        var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy" };
        return DateTime.TryParseExact(
            CleanCell(value),
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out date);
    }

    private static bool TryParseAmount(ImportCell value, out decimal amount)
    {
        if (value.Value is double doubleValue)
        {
            amount = Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
            return true;
        }

        if (value.Value is decimal decimalValue)
        {
            amount = decimalValue;
            return true;
        }

        var normalized = CleanCell(value).Replace(".", string.Empty).Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount);
    }

    private static List<List<ImportCell>> ReadCsvRecords(string text)
    {
        var records = new List<List<ImportCell>>();
        var record = new List<ImportCell>();
        var cell = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var current = text[i];
            if (current == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (!inQuotes && current == ';')
            {
                record.Add(new ImportCell(cell.ToString()));
                cell.Clear();
                continue;
            }

            if (!inQuotes && (current == '\n' || current == '\r'))
            {
                if (current == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                record.Add(new ImportCell(cell.ToString()));
                records.Add(record);
                record = [];
                cell.Clear();
                continue;
            }

            cell.Append(current);
        }

        record.Add(new ImportCell(cell.ToString()));
        if (record.Count > 1 || !string.IsNullOrWhiteSpace(CleanCell(record[0]))) records.Add(record);
        return records;
    }

    private static List<List<ImportCell>> ReadExcelRecords(byte[] file)
    {
        using var stream = new MemoryStream(file);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var records = new List<List<ImportCell>>();

        do
        {
            while (reader.Read())
            {
                var record = new List<ImportCell>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    record.Add(new ImportCell(reader.GetValue(i)));
                }
                records.Add(record);
            }
        } while (reader.NextResult() && records.Count == 0);

        return records;
    }
}

internal readonly record struct ImportCell(object? Value);

public static class BankMovementImportRowStatus
{
    public const string Valid = "valid";
    public const string Duplicate = "duplicate";
    public const string Warning = "warning";
    public const string Invalid = "invalid";
}

public static class BankMovementDuplicatePolicy
{
    public const string Skip = "skip";
    public const string ImportAnyway = "import-anyway";
}

public record BankMovementImportPreviewResponse(
    List<BankMovementImportRow> Rows,
    BankMovementImportSummary Summary);

public record BankMovementImportSummary(
    int TotalRows,
    int Valid,
    int Duplicates,
    int Invalid,
    int Warnings);

public record BankMovementImportRow(
    int RowNumber,
    string? Date,
    string? Description,
    decimal? Amount,
    string Status,
    string? DuplicateOfBankMovementId,
    List<string> Errors,
    List<string>? Headers)
{
    public bool Selected { get; init; } = true;
}

public record BankMovementImportCommitRequest(
    List<BankMovementImportRow> Rows,
    string DuplicatePolicy = BankMovementDuplicatePolicy.Skip);

public record BankMovementImportFailure(int RowNumber, string Error);

public record BankMovementImportCommitResponse(
    List<BankMovementResponse> Created,
    int SkippedDuplicates,
    List<BankMovementImportFailure> Failed);
