using Application.Inerfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public class LogHistoriesController : ControllerBase
    {
        private readonly ILogHistoryService _service;

        public LogHistoriesController(ILogHistoryService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("entity/{entityName}/{entityId}")]
        public async Task<IActionResult> ByEntity(string entityName, int entityId)
            => Ok(await _service.GetLogsByEntityAsync(entityName, entityId));

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> ByUser(string userId)
            => Ok(await _service.GetLogsByUserAsync(userId));

        [HttpGet("date-range")]
        public async Task<IActionResult> ByDateRange(
            [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
            => Ok(await _service.GetLogsByDateRangeAsync(startDate, endDate));
    }
}
