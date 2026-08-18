using System.Security.Claims;
using CashClarity.Api.Domain;
using CashClarity.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

[Authorize]
[ApiController]
[Route("server/bank-movements")]
public class BankMovementsController(IBankMovementsRepository repo) : ControllerBase
{
    private string UserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("Missing user id claim");

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
