using System.ComponentModel;
using CashClarity.Api.Contracts;
using CashClarity.Api.Repositories;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CashClarity.Api.Mcp;

[McpServerToolType]
public static class AccountsTools
{
    private const string AccountTypes =
        "Tipos de cuenta: 'main' (cuenta bancaria principal), 'space' (espacio/hucha donde se reserva saldo comprometido), " +
        "'entity' (entidad: pagador o contraparte), 'uncategorized' (sin categorizar).";

    [McpServerTool(Name = "list_accounts", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lista todas las cuentas contables del usuario: cuenta principal, espacios (huchas), entidades y sin categorizar. " +
                 AccountTypes + " La app calcula el saldo de cada cuenta como suma de (debit - credit) de sus líneas en list_journal_entries; " +
                 "saldo comprometido = suma de saldos de cuentas 'space'; disponible = saldo de 'main'; saldo bancario real = disponible + comprometido. " +
                 "Equivale a GET /server/workspaces/{workspaceId}/accounts.")]
    public static async Task<List<AccountResponse>> ListAccounts(
        RequestContext<CallToolRequestParams> context,
        IAccountsRepository repo,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId) =>
        await repo.GetAccounts(context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));

    [McpServerTool(Name = "create_account", Destructive = false, OpenWorld = false)]
    [Description("Crea una cuenta contable (un espacio, una entidad u otra cuenta). " + AccountTypes + " Equivale a POST /server/workspaces/{workspaceId}/accounts. Devuelve la cuenta creada.")]
    public static async Task<AccountResponse> CreateAccount(
        RequestContext<CallToolRequestParams> context,
        IAccountsRepository repo,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Código contable único, p. ej. '1001'.")] string code,
        [Description("Nombre visible, p. ej. 'Vacaciones'.")] string name,
        [Description("Tipo de cuenta: 'main', 'space', 'entity' o 'uncategorized'.")] AccountType type,
        [Description("Valor del campo balance almacenado. Por defecto 0.")] decimal balance = 0,
        [Description("Marca la cuenta como de sistema (no editable desde la app). Normalmente se omite.")] bool? isSystem = null) =>
        await repo.AddAccount(new AccountCreateRequest(code, name, type.ToApiValue(), balance, isSystem), context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));

    [McpServerTool(Name = "update_account", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Modifica parcialmente una cuenta: solo cambian los campos indicados. Equivale a PATCH /server/workspaces/{workspaceId}/accounts/{id}.")]
    public static async Task<object> UpdateAccount(
        RequestContext<CallToolRequestParams> context,
        IAccountsRepository repo,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Id de la cuenta (obtenido con list_accounts).")] string id,
        [Description("Nuevo código contable.")] string? code = null,
        [Description("Nuevo nombre.")] string? name = null,
        [Description("Nuevo tipo: 'main', 'space', 'entity' o 'uncategorized'.")] AccountType? type = null,
        [Description("Nuevo saldo.")] decimal? balance = null,
        [Description("Nuevo valor de cuenta de sistema.")] bool? isSystem = null)
    {
        await repo.UpdateAccount(id, new AccountPatchRequest(code, name, type?.ToApiValue(), balance, isSystem), context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));
        return new { success = true };
    }

    [McpServerTool(Name = "delete_account", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Elimina una cuenta. Falla si la cuenta todavía tiene líneas de asiento que la referencian. Equivale a DELETE /server/workspaces/{workspaceId}/accounts/{id}.")]
    public static async Task<object> DeleteAccount(
        RequestContext<CallToolRequestParams> context,
        IAccountsRepository repo,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Id de la cuenta a eliminar.")] string id)
    {
        await repo.DeleteAccount(id, context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));
        return new { success = true };
    }
}
