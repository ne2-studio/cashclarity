using System.ComponentModel;
using CashClarity.Api.Contracts;
using CashClarity.Api.Repositories;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CashClarity.Api.Mcp;

[McpServerToolType]
public static class JournalEntriesTools
{
    private const string LinesRule =
        "Contabilidad de partida doble: la suma de 'debit' debe ser igual a la suma de 'credit' entre todas las líneas, o el asiento se rechaza. " +
        "Ejemplo, reservar 100 € en un espacio: línea debit 100 en el espacio + línea credit 100 en la cuenta principal.";

    [McpServerTool(Name = "list_journal_entries", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lista todos los asientos contables del usuario con sus líneas (cuenta, debe, haber). Equivale a GET /server/workspaces/{workspaceId}/journal-entries.")]
    public static async Task<List<JournalEntryResponse>> ListJournalEntries(
        RequestContext<CallToolRequestParams> context,
        IJournalEntriesRepository repo,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId) =>
        await repo.GetJournalEntries(context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));

    [McpServerTool(Name = "create_journal_entry", Destructive = false, OpenWorld = false)]
    [Description("Crea un asiento contable. Los saldos de las cuentas se derivan de los asientos. " + LinesRule + " Equivale a POST /server/workspaces/{workspaceId}/journal-entries.")]
    public static async Task<JournalEntryResponse> CreateJournalEntry(
        RequestContext<CallToolRequestParams> context,
        IJournalEntriesRepository repo,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Fecha del asiento en formato ISO 8601, p. ej. '2026-10-24'.")] string date,
        [Description("Líneas del asiento (mínimo dos).")] List<JournalLineRequest> lines,
        [Description("Concepto del asiento.")] string? description = null) =>
        await repo.AddJournalEntry(new JournalEntryCreateRequest(date, description, lines), context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));

    [McpServerTool(Name = "update_journal_entry", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Modifica parcialmente un asiento. Si se pasan 'lines', sustituyen por completo a las existentes y deben cuadrar. " + LinesRule +
                 " Equivale a PATCH /server/workspaces/{workspaceId}/journal-entries/{id}.")]
    public static async Task<object> UpdateJournalEntry(
        RequestContext<CallToolRequestParams> context,
        IJournalEntriesRepository repo,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Id del asiento (obtenido con list_journal_entries).")] string id,
        [Description("Nueva fecha ISO 8601.")] string? date = null,
        [Description("Nuevo concepto.")] string? description = null,
        [Description("Nuevas líneas; sustituyen a todas las anteriores.")] List<JournalLineRequest>? lines = null)
    {
        await repo.UpdateJournalEntry(id, new JournalEntryPatchRequest(date, description, lines), context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));
        return new { success = true };
    }

    [McpServerTool(Name = "delete_journal_entry", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Elimina un asiento; deja de contar en los saldos derivados. Equivale a DELETE /server/workspaces/{workspaceId}/journal-entries/{id}.")]
    public static async Task<object> DeleteJournalEntry(
        RequestContext<CallToolRequestParams> context,
        IJournalEntriesRepository repo,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Id del asiento a eliminar.")] string id)
    {
        await repo.DeleteJournalEntry(id, context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));
        return new { success = true };
    }
}
