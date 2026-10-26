using CashClarity.Api.Contracts;
using CashClarity.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

[Authorize]
[ApiController]
[Route("server/workspaces/{workspaceId:guid}/bank-link")]
public class BankLinkController(IBankLinkService bankLinks) : WorkspaceScopedController
{
    [HttpGet]
    public async Task<IActionResult> GetBankLink() => Ok(await bankLinks.GetBankLink(UserId, WorkspaceId));

    [HttpGet("aspsps")]
    public async Task<IActionResult> GetAspsps([FromQuery] string country = "ES") => Ok(await bankLinks.GetAspsps(country));

    [HttpPost("authorizations")]
    public async Task<IActionResult> StartAuthorization([FromBody] BankAuthorizationStartRequest body) =>
        Ok(await bankLinks.StartAuthorization(body, UserId, WorkspaceId));

    [HttpPost("sessions")]
    public async Task<IActionResult> CompleteAuthorization([FromBody] BankAuthorizationCompleteRequest body) =>
        Ok(await bankLinks.CompleteAuthorization(body, UserId, WorkspaceId));

    [HttpPost("account")]
    public async Task<IActionResult> SelectAccount([FromBody] BankAccountSelectRequest body) =>
        Ok(await bankLinks.SelectAccount(body, UserId, WorkspaceId));

    [HttpPost("sync")]
    public async Task<IActionResult> Sync() => Ok(await bankLinks.Sync(UserId, WorkspaceId));

    [HttpDelete]
    public async Task<IActionResult> Unlink()
    {
        await bankLinks.Unlink(UserId, WorkspaceId);
        return Ok(new { success = true });
    }
}
