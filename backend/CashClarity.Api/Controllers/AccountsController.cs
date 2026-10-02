using CashClarity.Api.Contracts;
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
        return Ok(await repo.GetAccounts(UserId));
    }

    [HttpPost]
    public async Task<IActionResult> AddAccount([FromBody] AccountCreateRequest body)
    {
        return Ok(await repo.AddAccount(body, UserId));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateAccount(string id, [FromBody] AccountPatchRequest body)
    {
        await repo.UpdateAccount(id, body, UserId);
        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAccount(string id)
    {
        await repo.DeleteAccount(id, UserId);
        return Ok(new { success = true });
    }
}
