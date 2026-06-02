using Microsoft.AspNetCore.Mvc;
using NewbieCoder.Core.ViewModels;

namespace NewbieCoder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<string>> Get() =>
        Ok(ApiResponse<string>.Ok("OK", "API is running"));
}
