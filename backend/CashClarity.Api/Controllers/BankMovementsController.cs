using CashClarity.Api.Contracts;
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
        return Ok(await repo.GetBankMovements(UserId));
    }

    [HttpPost]
    public async Task<IActionResult> AddBankMovement([FromBody] BankMovementCreateRequest body)
    {
        return Ok(await repo.AddBankMovement(body, UserId));
    }

    [HttpPost("imports/preview")]
    public async Task<IActionResult> PreviewImport(IFormFile file)
    {
        if (file.Length == 0) return BadRequest(new { error = "CSV file is required" });
        await using var stream = file.OpenReadStream();
        return Ok(await imports.Preview(stream, UserId));
    }

    [HttpPost("imports")]
    public async Task<IActionResult> CommitImport([FromBody] BankMovementImportCommitRequest body)
    {
        return Ok(await imports.Commit(body, UserId));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateBankMovement(string id, [FromBody] BankMovementPatchRequest body)
    {
        await repo.UpdateBankMovement(id, body, UserId);
        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBankMovement(string id)
    {
        await repo.DeleteBankMovement(id, UserId);
        return Ok(new { success = true });
    }
}
