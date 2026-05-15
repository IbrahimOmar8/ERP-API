using Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HealthController(ApplicationDbContext context) => _context = context;

        // Liveness — does the process answer at all?
        [HttpGet]
        public IActionResult Get() => Ok(new
        {
            status = "ok",
            timeUtc = DateTime.UtcNow,
            version = typeof(HealthController).Assembly.GetName().Version?.ToString() ?? "1.0.0",
        });

        // Readiness — can we talk to the database?
        [HttpGet("ready")]
        public async Task<IActionResult> Ready()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                    return StatusCode(503, new { status = "down", reason = "database unreachable" });

                var products = await _context.Products.CountAsync();
                var users = await _context.Users.CountAsync();
                return Ok(new
                {
                    status = "ok",
                    database = "connected",
                    counts = new { products, users },
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "down", reason = ex.Message });
            }
        }
    }
}
