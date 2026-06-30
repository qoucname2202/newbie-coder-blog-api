using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewbieCoder.API.Extensions;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Controllers;

/// <summary>
/// Health check endpoints for verifying the API process is running.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Health")]
public class HealthController(IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    /// <summary>
    /// Returns a simple success payload when the API is alive.
    /// HTTP 200 OK with standard response envelope.
    /// </summary>
    [HttpGet]
    public ActionResult<ApiResponse<string>> Get()
    {
        var requestTrace = httpContextAccessor.HttpContext!.GetRequestTrace();
        return Ok(ApiResponse<string>.Success("OK", requestTrace));
    }
}
