using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace CashClarity.Api.Controllers;

public abstract class BaseController : ControllerBase
{
    protected string UserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("Missing user id claim");
}
