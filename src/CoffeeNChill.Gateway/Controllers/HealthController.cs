using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Checks that the gateway is running. Used by Postman and the VPS checks.
        /// </summary>
        /// <response code="200">The gateway is running.</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Get()
        {
            return Ok(new { status = "Healthy", timeUtc = DateTime.UtcNow });
        }
    }
}