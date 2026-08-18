using CashClarity.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

[Authorize]
[ApiController]
[Route("server/accounts")]
public class AccountsController(IAccountsRepository repo) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAccounts()
    {
        try { return Ok(await repo.GetAccounts(UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> AddAccount([FromBody] AccountCreateRequest body)
    {
        try { return Ok(await repo.AddAccount(body, UserId)); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateAccount(string id, [FromBody] AccountPatchRequest body)
    {
        try { await repo.UpdateAccount(id, body, UserId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(string id)
    {
        try { await repo.DeleteAccount(id, UserId); return Ok(new { success = true }); }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }
}

public record AccountResponse(string Id, string Code, string Name, string Type, decimal Balance, bool IsSystem, string UserId);
public record AccountCreateRequest(string Code, string Name, string Type, decimal Balance = 0, bool? IsSystem = null);
public record AccountPatchRequest(string? Code = null, string? Name = null, string? Type = null, decimal? Balance = null, bool? IsSystem = null);
