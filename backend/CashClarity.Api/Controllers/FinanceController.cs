using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using CashClarity.Api.Domain;
using CashClarity.Api.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace CashClarity.Api.Controllers;

[Authorize]
[ApiController]
[Route("server")]
public class FinanceController(FinanceRepository repo) : ControllerBase
{
    private string UserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("Missing user id claim");

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "ok" });
    }

    // Accounts

    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        try { return Ok(await repo.GetAccounts(UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost("accounts")]
    public async Task<IActionResult> AddAccount([FromBody] AccountCreateRequest body)
    {
        try { return Ok(await repo.AddAccount(body, UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPatch("accounts/{id}")]
    public async Task<IActionResult> UpdateAccount(string id, [FromBody] AccountPatchRequest body)
    {
        try { await repo.UpdateAccount(id, body, UserId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpDelete("accounts/{id}")]
    public async Task<IActionResult> DeleteAccount(string id)
    {
        try { await repo.DeleteAccount(id, UserId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // Journal Entries

    [HttpGet("journal-entries")]
    public async Task<IActionResult> GetJournalEntries()
    {
        try { return Ok(await repo.GetJournalEntries(UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost("journal-entries")]
    public async Task<IActionResult> AddJournalEntry([FromBody] JournalEntryCreateRequest body)
    {
        try { return Ok(await repo.AddJournalEntry(body, UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPatch("journal-entries/{id}")]
    public async Task<IActionResult> UpdateJournalEntry(string id, [FromBody] JournalEntryPatchRequest body)
    {
        try { await repo.UpdateJournalEntry(id, body, UserId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpDelete("journal-entries/{id}")]
    public async Task<IActionResult> DeleteJournalEntry(string id)
    {
        try { await repo.DeleteJournalEntry(id, UserId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    // Bank Movements

    [HttpGet("bank-movements")]
    public async Task<IActionResult> GetBankMovements()
    {
        try { return Ok(await repo.GetBankMovements(UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost("bank-movements")]
    public async Task<IActionResult> AddBankMovement([FromBody] BankMovementCreateRequest body)
    {
        try { return Ok(await repo.AddBankMovement(body, UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPatch("bank-movements/{id}")]
    public async Task<IActionResult> UpdateBankMovement(string id, [FromBody] BankMovementPatchRequest body)
    {
        try { await repo.UpdateBankMovement(id, body, UserId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpDelete("bank-movements/{id}")]
    public async Task<IActionResult> DeleteBankMovement(string id)
    {
        try { await repo.DeleteBankMovement(id, UserId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }
}
