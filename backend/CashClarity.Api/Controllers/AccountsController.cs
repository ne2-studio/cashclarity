using CashClarity.Api.Contracts;
using CashClarity.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

[Authorize]
[ApiController]
[Route("server/workspaces/{workspaceId:guid}/accounts")]
public class AccountsController(IAccountsRepository repo) : WorkspaceScopedController
{
    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        try { return Ok(await repo.GetAccounts(UserId, WorkspaceId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> AddAccount([FromBody] AccountCreateRequest body)
    {
        try { return Ok(await repo.AddAccount(body, UserId, WorkspaceId)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateAccount(string id, [FromBody] AccountPatchRequest body)
    {
        try { await repo.UpdateAccount(id, body, UserId, WorkspaceId); return Ok(new { success = true }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(string id)
    {
        try { await repo.DeleteAccount(id, UserId, WorkspaceId); return Ok(new { success = true }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }
}
