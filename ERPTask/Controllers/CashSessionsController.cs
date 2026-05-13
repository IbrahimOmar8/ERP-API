using System.Security.Claims;
using Application.DTOs.POS;
using Application.Inerfaces.POS;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier},{Roles.Accountant}")]
    public class CashSessionsController : ControllerBase
    {
        private readonly ICashSessionService _service;
        public CashSessionsController(ICashSessionService service) => _service = service;

        private Guid CurrentUserId
        {
            get
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
                return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
            }
        }

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id) =>
            (await _service.GetByIdAsync(id)) is { } s ? Ok(s) : NotFound();

        [HttpGet("current")]
        public async Task<IActionResult> Current() =>
            (await _service.GetCurrentSessionAsync(CurrentUserId)) is { } s ? Ok(s) : NotFound();

        [HttpPost("open")]
        public async Task<IActionResult> Open(OpenSessionDto dto)
        {
            try { return Ok(await _service.OpenAsync(dto, CurrentUserId)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("{id}/close")]
        public async Task<IActionResult> Close(Guid id, CloseSessionDto dto) =>
            (await _service.CloseAsync(id, dto)) is { } s ? Ok(s) : NotFound();
    }
}
