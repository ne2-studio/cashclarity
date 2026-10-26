using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

[ServiceFilter(typeof(WorkspaceOwnershipFilter))]
public abstract class WorkspaceScopedController : BaseController
{
    /// <summary>Verified by <see cref="WorkspaceOwnershipFilter"/> before any action runs.</summary>
    protected Guid WorkspaceId => Guid.Parse((string)RouteData.Values["workspaceId"]!);
}
