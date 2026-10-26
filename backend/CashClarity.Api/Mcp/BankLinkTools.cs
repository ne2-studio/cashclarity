using System.ComponentModel;
using CashClarity.Api.Contracts;
using CashClarity.Api.Repositories;
using CashClarity.Api.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CashClarity.Api.Mcp;

[McpServerToolType]
public static class BankLinkTools
{
    [McpServerTool(Name = "get_bank_link", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Devuelve el vínculo del workspace con su cuenta bancaria real (Enable Banking): 'status' es none, pending, awaiting-account, " +
                 "active o expired; 'validUntil' es la caducidad del consentimiento y 'lastSyncedAt' la última sincronización. " +
                 "Equivale a GET /server/workspaces/{workspaceId}/bank-link.")]
    public static async Task<BankLinkResponse> GetBankLink(
        RequestContext<CallToolRequestParams> context,
        IBankLinkService bankLinks,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId) =>
        await bankLinks.GetBankLink(context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));

    [McpServerTool(Name = "list_bank_aspsps", ReadOnly = true, Idempotent = true, OpenWorld = true)]
    [Description("Lista los bancos disponibles en Enable Banking para un país, con los tipos de cliente que admiten. " +
                 "Equivale a GET /server/workspaces/{workspaceId}/bank-link/aspsps.")]
    public static async Task<List<BankAspspResponse>> ListBankAspsps(
        RequestContext<CallToolRequestParams> context,
        IBankLinkService bankLinks,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Código ISO del país, p. ej. 'ES'.")] string country = "ES")
    {
        await context.OwnedWorkspace(workspaces, workspaceId);
        return await bankLinks.GetAspsps(country);
    }

    [McpServerTool(Name = "start_bank_authorization", Destructive = false, OpenWorld = true)]
    [Description("Inicia la vinculación con un banco y devuelve la URL de login del banco, que el usuario debe abrir en el navegador. " +
                 "El banco vuelve a la app web, que completa la vinculación. Equivale a POST /server/workspaces/{workspaceId}/bank-link/authorizations.")]
    public static async Task<BankAuthorizationStartResponse> StartBankAuthorization(
        RequestContext<CallToolRequestParams> context,
        IBankLinkService bankLinks,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Nombre del banco tal como lo devuelve list_bank_aspsps.")] string aspspName,
        [Description("País del banco, p. ej. 'ES'.")] string aspspCountry,
        [Description("Tipo de cliente: 'personal' o 'business'.")] string psuType,
        [Description("Fecha ISO 8601 desde la que sincronizar. Por defecto, el día siguiente al último movimiento.")] string? syncFrom = null) =>
        await bankLinks.StartAuthorization(
            new BankAuthorizationStartRequest(aspspName, aspspCountry, psuType, syncFrom),
            context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));

    [McpServerTool(Name = "complete_bank_authorization", Destructive = false, OpenWorld = true)]
    [Description("Completa la vinculación con el 'code' y el 'state' con los que el banco redirige tras el login. " +
                 "Equivale a POST /server/workspaces/{workspaceId}/bank-link/sessions.")]
    public static async Task<BankLinkResponse> CompleteBankAuthorization(
        RequestContext<CallToolRequestParams> context,
        IBankLinkService bankLinks,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("Parámetro 'code' de la URL de vuelta del banco.")] string code,
        [Description("Parámetro 'state' de la URL de vuelta del banco.")] string state) =>
        await bankLinks.CompleteAuthorization(
            new BankAuthorizationCompleteRequest(code, state),
            context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));

    [McpServerTool(Name = "select_bank_account", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Elige la cuenta a sincronizar cuando la autorización trae varias (status 'awaiting-account'). " +
                 "Equivale a POST /server/workspaces/{workspaceId}/bank-link/account.")]
    public static async Task<BankLinkResponse> SelectBankAccount(
        RequestContext<CallToolRequestParams> context,
        IBankLinkService bankLinks,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId,
        [Description("'uid' de una de las 'accountOptions' de get_bank_link.")] string accountUid) =>
        await bankLinks.SelectAccount(
            new BankAccountSelectRequest(accountUid),
            context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));

    [McpServerTool(Name = "sync_bank_link", Destructive = false, Idempotent = true, OpenWorld = true)]
    [Description("Descarga del banco los movimientos contabilizados desde la última sincronización y crea los que falten, sin identificar. " +
                 "Es idempotente: repetirla no duplica. 'possibleDuplicates' avisa de coincidencias con movimientos manuales o de CSV. " +
                 "Equivale a POST /server/workspaces/{workspaceId}/bank-link/sync.")]
    public static async Task<BankSyncResponse> SyncBankLink(
        RequestContext<CallToolRequestParams> context,
        IBankLinkService bankLinks,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId) =>
        await bankLinks.Sync(context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));

    [McpServerTool(Name = "unlink_bank", Destructive = true, Idempotent = true, OpenWorld = true)]
    [Description("Desvincula la cuenta bancaria y cierra la sesión en Enable Banking. Conserva los movimientos ya sincronizados. " +
                 "Equivale a DELETE /server/workspaces/{workspaceId}/bank-link.")]
    public static async Task<object> UnlinkBank(
        RequestContext<CallToolRequestParams> context,
        IBankLinkService bankLinks,
        IWorkspacesRepository workspaces,
        [Description(McpRequestContextExtensions.WorkspaceIdDescription)] string workspaceId)
    {
        await bankLinks.Unlink(context.UserId(), await context.OwnedWorkspace(workspaces, workspaceId));
        return new { success = true };
    }
}
