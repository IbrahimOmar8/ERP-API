using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockController : ControllerBase
    {
        private readonly IStockService _service;
        public StockController(IStockService service) => _service = service;

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
        public async Task<IActionResult> Adjust(StockAdjustmentDto dto)
        {
            await _service.AdjustStockAsync(dto, null);
            return Ok();
        }
    }
}
