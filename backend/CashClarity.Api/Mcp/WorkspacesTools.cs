using System.ComponentModel;
using CashClarity.Api.Contracts;
using CashClarity.Api.Repositories;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CashClarity.Api.Mcp;

[McpServerToolType]
public static class WorkspacesTools
{
    [McpServerTool(Name = "list_workspaces", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lista los workspaces del usuario. Cada workspace es un extracto bancario independiente con sus cuentas, espacios, asientos y movimientos; " +
                 "el resto de tools exige un 'workspaceId'. Equivale a GET /server/workspaces.")]
    public static Task<List<WorkspaceResponse>> ListWorkspaces(RequestContext<CallToolRequestParams> context, IWorkspacesRepository repo) =>
        repo.GetWorkspaces(context.UserId());

    [McpServerTool(Name = "create_workspace", Destructive = false, OpenWorld = false)]
    [Description("Crea un workspace nuevo y vacío. Equivale a POST /server/workspaces. Devuelve el workspace creado.")]
    public static Task<WorkspaceResponse> CreateWorkspace(
        RequestContext<CallToolRequestParams> context,
        IWorkspacesRepository repo,
        [Description("Nombre del workspace. Si se omite se usa el nombre por defecto.")] string? name = null) =>
        repo.CreateWorkspace(name, context.UserId());
}
