using System.Security.Claims;
using CashClarity.Api.Domain;
using CashClarity.Api.Repositories;
using ModelContextProtocol.Server;

namespace CashClarity.Api.Mcp;

public static class McpRequestContextExtensions
{
    public const string WorkspaceIdDescription = "Id del workspace (obtenido con list_workspaces). Cada workspace tiene su propio extracto, cuentas y asientos.";

    /// <summary>
    /// Same identity rule as <see cref="Controllers.BaseController"/>: the OIDC subject claim scopes every query.
    /// </summary>
    public static string UserId<TParams>(this RequestContext<TParams> context) =>
        context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("Missing user id claim");

    /// <summary>
    /// Same rule as <see cref="Controllers.WorkspaceOwnershipFilter"/>: a workspace the caller does not own is reported
    /// as not found so other users' workspaces are not leaked.
    /// </summary>
    public static async Task<Guid> OwnedWorkspace<TParams>(this RequestContext<TParams> context, IWorkspacesRepository workspaces, string workspaceId)
    {
        if (!Guid.TryParse(workspaceId, out var id) || !await workspaces.UserOwnsWorkspace(id, context.UserId()))
            throw new NotFoundException("Workspace not found");
        return id;
    }
}
