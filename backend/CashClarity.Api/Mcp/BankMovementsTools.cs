using System.ComponentModel;
using System.Text;
using System.Text.Json.Serialization;
using CashClarity.Api.Contracts;
using CashClarity.Api.Repositories;
using CashClarity.Api.Services;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CashClarity.Api.Mcp;

[JsonConverter(typeof(JsonStringEnumConverter<DuplicatePolicy>))]
public enum DuplicatePolicy
{
    [JsonStringEnumMemberName(BankMovementDuplicatePolicy.Skip)] Skip,
    [JsonStringEnumMemberName(BankMovementDuplicatePolicy.ImportAnyway)] ImportAnyway,
}

[McpServerToolType]
public static class BankMovementsTools
{
    [McpServerTool(Name = "list_bank_movements", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lista los movimientos del extracto bancario del usuario. 'isIdentified' indica si ya está conciliado; 'entityId' es la cuenta de tipo 'entity' " +
                 "asociada y 'journalEntryId' el asiento que lo concilia. Equivale a GET /server/workspaces/{workspaceId}/bank-movements.")]
    public static async Task<List<BankMovementResponse>> ListBankMovements(
        RequestContext<CallToolRequestParams> context,
        IBankMovementsRepository repo,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId) =>
        await repo.GetBankMovements(context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));

    [McpServerTool(Name = "create_bank_movement", Destructive = false, OpenWorld = false)]
    [Description("Crea un movimiento bancario manual. Importe positivo = ingreso, negativo = cargo. Equivale a POST /server/workspaces/{workspaceId}/bank-movements.")]
    public static async Task<BankMovementResponse> CreateBankMovement(
        RequestContext<CallToolRequestParams> context,
        IBankMovementsRepository repo,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Fecha ISO 8601, p. ej. '2026-10-24'.")] string date,
        [Description("Concepto del movimiento.")] string description,
        [Description("Importe con signo: positivo ingreso, negativo cargo.")] decimal amount,
        [Description("Id de la cuenta 'entity' (contraparte) asociada.")] string? entityId = null,
        [Description("Id del asiento que concilia el movimiento.")] string? journalEntryId = null) =>
        await repo.AddBankMovement(new BankMovementCreateRequest(date, description, amount, entityId, journalEntryId), context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));

    [McpServerTool(Name = "update_bank_movement", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Modifica parcialmente un movimiento bancario; se usa para conciliarlo (isIdentified, entityId, journalEntryId). " +
                 "Equivale a PATCH /server/workspaces/{workspaceId}/bank-movements/{id}.")]
    public static async Task<object> UpdateBankMovement(
        RequestContext<CallToolRequestParams> context,
        IBankMovementsRepository repo,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Id del movimiento (obtenido con list_bank_movements).")] string id,
        [Description("Nueva fecha ISO 8601.")] string? date = null,
        [Description("Nuevo concepto.")] string? description = null,
        [Description("Nuevo importe con signo.")] decimal? amount = null,
        [Description("Marca el movimiento como identificado/conciliado.")] bool? isIdentified = null,
        [Description("Id de la cuenta 'entity' asociada.")] string? entityId = null,
        [Description("Id del asiento que concilia el movimiento.")] string? journalEntryId = null)
    {
        await repo.UpdateBankMovement(
            id, new BankMovementPatchRequest(date, description, amount, isIdentified, entityId, journalEntryId), context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));
        return new { success = true };
    }

    [McpServerTool(Name = "delete_bank_movement", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Elimina un movimiento bancario. Equivale a DELETE /server/workspaces/{workspaceId}/bank-movements/{id}.")]
    public static async Task<object> DeleteBankMovement(
        RequestContext<CallToolRequestParams> context,
        IBankMovementsRepository repo,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Id del movimiento a eliminar.")] string id)
    {
        await repo.DeleteBankMovement(id, context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));
        return new { success = true };
    }

    [McpServerTool(Name = "preview_bank_movement_import", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Analiza un extracto bancario (CSV o Excel .xls/.xlsx) sin guardar nada. Devuelve cada fila con estado " +
                 "'valid', 'warning', 'duplicate' o 'invalid' y un resumen. Pasa exactamente uno de 'csvText' o 'fileBase64'. " +
                 "Después usa commit_bank_movement_import con las filas devueltas. Equivale a POST /server/workspaces/{workspaceId}/bank-movements/imports/preview.")]
    public static async Task<BankMovementImportPreviewResponse> PreviewBankMovementImport(
        RequestContext<CallToolRequestParams> context,
        IBankMovementImportService imports,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Contenido CSV como texto. Debe tener columnas de fecha, concepto/movimiento e importe/cantidad.")] string? csvText = null,
        [Description("Contenido del archivo (CSV o Excel) codificado en base64.")] string? fileBase64 = null)
    {
        var content = DecodeImportFile(csvText, fileBase64);
        using var stream = new MemoryStream(content);
        return await imports.Preview(stream, context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));
    }

    [McpServerTool(Name = "commit_bank_movement_import", Destructive = false, OpenWorld = false)]
    [Description("Importa las filas seleccionadas de una previsualización como movimientos bancarios. Las filas con selected=false se ignoran; " +
                 "las 'invalid' fallan; los duplicados se saltan o se importan según 'duplicatePolicy'. " +
                 "Equivale a POST /server/workspaces/{workspaceId}/bank-movements/imports.")]
    public static async Task<BankMovementImportCommitResponse> CommitBankMovementImport(
        RequestContext<CallToolRequestParams> context,
        IBankMovementImportService imports,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Filas tal como las devolvió preview_bank_movement_import, opcionalmente con 'selected' a false.")] List<BankMovementImportRow> rows,
        [Description("Qué hacer con los duplicados: 'skip' (por defecto) o 'import-anyway'.")] DuplicatePolicy duplicatePolicy = DuplicatePolicy.Skip) =>
        await imports.Commit(
            new BankMovementImportCommitRequest(
                rows,
                duplicatePolicy == DuplicatePolicy.ImportAnyway ? BankMovementDuplicatePolicy.ImportAnyway : BankMovementDuplicatePolicy.Skip),
            context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));

    private static byte[] DecodeImportFile(string? csvText, string? fileBase64)
    {
        if (string.IsNullOrEmpty(csvText) == string.IsNullOrEmpty(fileBase64))
            throw new McpException("Indica exactamente uno de 'csvText' o 'fileBase64'.");
        if (csvText is not null && csvText.Length > 0) return Encoding.UTF8.GetBytes(csvText);
        try
        {
            return Convert.FromBase64String(fileBase64!);
        }
        catch (FormatException)
        {
            throw new McpException("'fileBase64' no es base64 válido.");
        }
    }
}
