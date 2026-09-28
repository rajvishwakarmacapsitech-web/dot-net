using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "ok",
            message = "Server is running"
        });
    }
}