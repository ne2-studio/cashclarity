using System.Security.Claims;
using CashClarity.Api.Domain;
using CashClarity.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

[Authorize]
[ApiController]
[Route("server/journal-entries")]
public class JournalEntriesController(IJournalEntriesRepository repo) : ControllerBase
{
    private string UserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("Missing user id claim");

    [HttpGet]
    public async Task<IActionResult> GetJournalEntries()
    {
        try { return Ok(await repo.GetJournalEntries(UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> AddJournalEntry([FromBody] JournalEntryCreateRequest body)
    {
        try { return Ok(await repo.AddJournalEntry(body, UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateJournalEntry(string id, [FromBody] JournalEntryPatchRequest body)
    {
        try { await repo.UpdateJournalEntry(id, body, UserId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJournalEntry(string id)
    {
        try { await repo.DeleteJournalEntry(id, UserId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }
}
