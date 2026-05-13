using System.Security.Claims;
using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.WarehouseKeeper}")]
    public class StockTransfersController : ControllerBase
    {
        private readonly IStockTransferService _service;
        public StockTransfersController(IStockTransferService service) => _service = service;

        private Guid? CurrentUserId =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value, out var id) ? id : null;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? warehouseId)
            => Ok(await _service.GetAllAsync(warehouseId));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
            => (await _service.GetByIdAsync(id)) is { } t ? Ok(t) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateStockTransferDto dto)
        {
            try { return Ok(await _service.CreateAsync(dto, CurrentUserId)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> Complete(Guid id)
        {
            try { return await _service.CompleteAsync(id, CurrentUserId) ? Ok() : NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            try { return await _service.CancelAsync(id) ? NoContent() : NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }
    }
}
