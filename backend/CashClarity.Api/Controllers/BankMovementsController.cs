using CashClarity.Api.Contracts;
using CashClarity.Api.Repositories;
using CashClarity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

[Authorize]
[ApiController]
[Route("server/workspaces/{workspaceId:guid}/bank-movements")]
public class BankMovementsController(IBankMovementsRepository repo, IBankMovementImportService imports) : WorkspaceScopedController
{
    [HttpGet]
    public async Task<IActionResult> GetBankMovements()
    {
        try { return Ok(await repo.GetBankMovements(UserId, WorkspaceId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> AddBankMovement([FromBody] BankMovementCreateRequest body)
    {
        try { return Ok(await repo.AddBankMovement(body, UserId, WorkspaceId)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost("imports/preview")]
    public async Task<IActionResult> PreviewImport(IFormFile file)
    {
        try
        {
            if (file.Length == 0) return BadRequest(new { error = "CSV file is required" });
            await using var stream = file.OpenReadStream();
            return Ok(await imports.Preview(stream, UserId, WorkspaceId));
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost("imports")]
    public async Task<IActionResult> CommitImport([FromBody] BankMovementImportCommitRequest body)
    {
        try { return Ok(await imports.Commit(body, UserId, WorkspaceId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateBankMovement(string id, [FromBody] BankMovementPatchRequest body)
    {
        try { await repo.UpdateBankMovement(id, body, UserId, WorkspaceId); return Ok(new { success = true }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBankMovement(string id)
    {
        try { await repo.DeleteBankMovement(id, UserId, WorkspaceId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }
}
