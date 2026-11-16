using LibraryHub.Data;
using LibraryHub.Shared;
using Microsoft.AspNetCore.Mvc;

namespace LibraryHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemController(ApplicationDbContext context) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;

        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck(CancellationToken token)
        {
            bool isHealthy = await _context.Database.CanConnectAsync(token);

            if (!isHealthy)
            {
                return Ok(
                    ApiResponse<string>.Fail(ApiMessage.Auth.SystemUnhealthy));
            }

            return Ok(
                ApiResponse<string>.Ok(ApiMessage.Auth.SystemHealthy));
        }
    }
}