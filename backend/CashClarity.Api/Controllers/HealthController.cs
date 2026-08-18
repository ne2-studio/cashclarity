using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

[ApiController]
[Route("server/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "ok" });
    }
}
