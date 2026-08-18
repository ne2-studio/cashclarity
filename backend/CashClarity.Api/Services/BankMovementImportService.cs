using System.Globalization;
using System.Text;
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
    public async Task<BankMovementImportPreviewResponse> Preview(Stream csv, string userId)
    {
        using var reader = new StreamReader(csv, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        var text = await reader.ReadToEndAsync();
        var parsedRows = Parse(text);
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

    private static List<BankMovementImportRow> Parse(string text)
    {
        var records = ReadCsvRecords(text);
        if (records.Count < 2)
        {
            return [InvalidFileRow("El archivo esta vacio o no tiene suficientes lineas.")];
        }

        var headers = records[0].Select(CleanHeader).ToList();
        var dateIndex = headers.IndexOf("fecha");
        var descriptionIndex = headers.IndexOf("concepto");
        var amountIndex = headers.IndexOf("cantidad");

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
        for (var i = 1; i < records.Count; i++)
        {
            var record = records[i];
            if (record.All(string.IsNullOrWhiteSpace)) continue;

            var dateText = GetCell(record, dateIndex);
            var description = GetCell(record, descriptionIndex);
            var amountText = GetCell(record, amountIndex);
            var errors = new List<string>();

            var date = TryParseDate(dateText, out var parsedDate)
                ? parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : null;
            if (date is null) errors.Add("Fecha invalida");
            if (string.IsNullOrWhiteSpace(description)) errors.Add("Concepto obligatorio");
            var amount = TryParseAmount(amountText, out var parsedAmount) ? parsedAmount : (decimal?)null;
            if (amount is null) errors.Add("Cantidad invalida");

            rows.Add(new BankMovementImportRow(
                i + 1,
                date,
                string.IsNullOrWhiteSpace(description) ? null : description,
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

    private static string CleanHeader(string value) => CleanCell(value).ToLowerInvariant();

    private static string GetCell(List<string> record, int index) =>
        index < record.Count ? CleanCell(record[index]) : string.Empty;

    private static string CleanCell(string value) => value.Trim().Trim('"', '\'').Trim();

    private static bool TryParseDate(string value, out DateTime date)
    {
        var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy" };
        return DateTime.TryParseExact(
            CleanCell(value),
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal,
            out date);
    }

    private static bool TryParseAmount(string value, out decimal amount)
    {
        var normalized = CleanCell(value).Replace(".", string.Empty).Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount);
    }

    private static List<List<string>> ReadCsvRecords(string text)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
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
                record.Add(cell.ToString());
                cell.Clear();
                continue;
            }

            if (!inQuotes && (current == '\n' || current == '\r'))
            {
                if (current == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                record.Add(cell.ToString());
                records.Add(record);
                record = [];
                cell.Clear();
                continue;
            }

            cell.Append(current);
        }

        record.Add(cell.ToString());
        if (record.Count > 1 || !string.IsNullOrWhiteSpace(record[0])) records.Add(record);
        return records;
    }
}

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
