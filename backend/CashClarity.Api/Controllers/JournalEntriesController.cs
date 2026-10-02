using CashClarity.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

[Authorize]
[ApiController]
[Route("server/journal-entries")]
public class JournalEntriesController(IJournalEntriesRepository repo) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetJournalEntries()
    {
        return Ok(await repo.GetJournalEntries(UserId));
    }

    [HttpPost]
    public async Task<IActionResult> AddJournalEntry([FromBody] JournalEntryCreateRequest body)
    {
        return Ok(await repo.AddJournalEntry(body, UserId));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateJournalEntry(string id, [FromBody] JournalEntryPatchRequest body)
    {
        await repo.UpdateJournalEntry(id, body, UserId);
        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteJournalEntry(string id)
    {
        await repo.DeleteJournalEntry(id, UserId);
        return Ok(new { success = true });
    }
}

public record JournalLineResponse(string Id, string AccountId, decimal Credit, decimal Debit, string? Description);
public record JournalEntryResponse(string Id, DateTime Date, string? Description, List<JournalLineResponse> Lines, string UserId);
public record JournalLineRequest(string AccountId, decimal Credit, decimal Debit, string? Description = null);
public record JournalEntryCreateRequest(string Date, string? Description, List<JournalLineRequest> Lines);
public record JournalEntryPatchRequest(string? Date = null, string? Description = null, List<JournalLineRequest>? Lines = null);
