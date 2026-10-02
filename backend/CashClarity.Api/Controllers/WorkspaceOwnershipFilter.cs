using CashClarity.Api.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CashClarity.Api.Controllers;

/// <summary>
/// Rejects requests whose {workspaceId} route value is not a workspace owned by the
/// authenticated user. Responds 404 so other users' workspaces are not leaked.
/// </summary>
public class WorkspaceOwnershipFilter(IWorkspacesRepository workspaces) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userId = context.HttpContext.User
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var raw = context.RouteData.Values["workspaceId"]?.ToString();

        if (userId is null || !Guid.TryParse(raw, out var workspaceId) ||
            !await workspaces.UserOwnsWorkspace(workspaceId, userId))
        {
            context.Result = new NotFoundObjectResult(new { error = "Workspace not found" });
            return;
        }

        await next();
    }
}
