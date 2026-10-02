using CashClarity.Api.Contracts;
using CashClarity.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

[Authorize]
[ApiController]
[Route("server/workspaces/{workspaceId:guid}/journal-entries")]
public class JournalEntriesController(IJournalEntriesRepository repo) : WorkspaceScopedController
{
    [HttpGet]
    public async Task<IActionResult> GetJournalEntries()
    {
        try { return Ok(await repo.GetJournalEntries(UserId, WorkspaceId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> AddJournalEntry([FromBody] JournalEntryCreateRequest body)
    {
        try { return Ok(await repo.AddJournalEntry(body, UserId, WorkspaceId)); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateJournalEntry(string id, [FromBody] JournalEntryPatchRequest body)
    {
        try { await repo.UpdateJournalEntry(id, body, UserId, WorkspaceId); return Ok(new { success = true }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJournalEntry(string id)
    {
        try { await repo.DeleteJournalEntry(id, UserId, WorkspaceId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }
}
