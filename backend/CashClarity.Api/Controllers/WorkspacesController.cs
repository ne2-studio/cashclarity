using CashClarity.Api.Contracts;
using CashClarity.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

[Authorize]
[ApiController]
[Route("server/workspaces")]
public class WorkspacesController(IWorkspacesRepository repo) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetWorkspaces()
    {
        return Ok(await repo.GetWorkspaces(UserId));
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkspace([FromBody] WorkspaceCreateRequest body)
    {
        return Ok(await repo.CreateWorkspace(body.Name, UserId));
    }
}
