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
    [Authorize]
    public class StockController : ControllerBase
    {
        private readonly IStockService _service;
        public StockController(IStockService service) => _service = service;

        private Guid? CurrentUserId =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value, out var id) ? id : null;

        [HttpGet("warehouse/{warehouseId}")]
        public async Task<IActionResult> ByWarehouse(Guid warehouseId)
            => Ok(await _service.GetStockByWarehouseAsync(warehouseId));

        [HttpGet("product/{productId}")]
        public async Task<IActionResult> ByProduct(Guid productId)
            => Ok(await _service.GetStockByProductAsync(productId));

        [HttpGet("balance")]
        public async Task<IActionResult> Balance([FromQuery] Guid productId, [FromQuery] Guid warehouseId)
        {
            var item = await _service.GetStockAsync(productId, warehouseId);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpGet("movements/{productId}")]
        public async Task<IActionResult> Movements(Guid productId, [FromQuery] Guid? warehouseId)
            => Ok(await _service.GetMovementsAsync(productId, warehouseId));

        [HttpPost("adjust")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.WarehouseKeeper}")]
        public async Task<IActionResult> Adjust(StockAdjustmentDto dto)
        {
            await _service.AdjustStockAsync(dto, CurrentUserId);
            return Ok();
        }
    }
}
