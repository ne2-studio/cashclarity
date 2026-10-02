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
        try { return Ok(await repo.GetWorkspaces(UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkspace([FromBody] WorkspaceCreateRequest body)
    {
        try { return Ok(await repo.CreateWorkspace(body.Name, UserId)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }
}
