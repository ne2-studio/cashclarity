using CashClarity.Api.Repositories;
using CashClarity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

[Authorize]
[ApiController]
[Route("server/bank-movements")]
public class BankMovementsController(IBankMovementsRepository repo, IBankMovementImportService imports) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetBankMovements()
    {
        try { return Ok(await repo.GetBankMovements(UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> AddBankMovement([FromBody] BankMovementCreateRequest body)
    {
        try { return Ok(await repo.AddBankMovement(body, UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost("imports/preview")]
    public async Task<IActionResult> PreviewImport(IFormFile file)
    {
        try
        {
            if (file.Length == 0) return BadRequest(new { error = "CSV file is required" });
            await using var stream = file.OpenReadStream();
            return Ok(await imports.Preview(stream, UserId));
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost("imports")]
    public async Task<IActionResult> CommitImport([FromBody] BankMovementImportCommitRequest body)
    {
        try { return Ok(await imports.Commit(body, UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateBankMovement(string id, [FromBody] BankMovementPatchRequest body)
    {
        try { await repo.UpdateBankMovement(id, body, UserId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBankMovement(string id)
    {
        try { await repo.DeleteBankMovement(id, UserId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }
}

public record BankMovementResponse(string Id, DateTime Date, string Description, decimal Amount, bool IsIdentified, string? EntityId, string? JournalEntryId, string UserId);
public record BankMovementCreateRequest(string Date, string Description, decimal Amount, string? EntityId = null, string? JournalEntryId = null);
public record BankMovementPatchRequest(string? Date = null, string? Description = null, decimal? Amount = null, bool? IsIdentified = null, string? EntityId = null, string? JournalEntryId = null);
