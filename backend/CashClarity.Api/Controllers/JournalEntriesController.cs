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
        return Ok(await repo.GetJournalEntries(UserId, WorkspaceId));
    }

    [HttpPost]
    public async Task<IActionResult> AddJournalEntry([FromBody] JournalEntryCreateRequest body)
    {
        return Ok(await repo.AddJournalEntry(body, UserId, WorkspaceId));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateJournalEntry(string id, [FromBody] JournalEntryPatchRequest body)
    {
        await repo.UpdateJournalEntry(id, body, UserId, WorkspaceId);
        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJournalEntry(string id)
    {
        await repo.DeleteJournalEntry(id, UserId, WorkspaceId);
        return Ok(new { success = true });
    }
}
