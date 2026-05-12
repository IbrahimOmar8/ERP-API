using Application.DTOs.POS;
using Application.Inerfaces.POS;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CashSessionsController : ControllerBase
    {
        private readonly ICashSessionService _service;
        public CashSessionsController(ICashSessionService service) => _service = service;

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id) =>
            (await _service.GetByIdAsync(id)) is { } s ? Ok(s) : NotFound();

        [HttpGet("current/{userId}")]
        public async Task<IActionResult> Current(Guid userId) =>
            (await _service.GetCurrentSessionAsync(userId)) is { } s ? Ok(s) : NotFound();

        [HttpPost("open/{userId}")]
        public async Task<IActionResult> Open(Guid userId, OpenSessionDto dto)
        {
            try { return Ok(await _service.OpenAsync(dto, userId)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("{id}/close")]
        public async Task<IActionResult> Close(Guid id, CloseSessionDto dto) =>
            (await _service.CloseAsync(id, dto)) is { } s ? Ok(s) : NotFound();
    }
}
