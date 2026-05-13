using Application.Inerfaces.Reports;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Accountant}")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _service;

        public ReportsController(IReportService service) => _service = service;

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard(CancellationToken ct)
            => Ok(await _service.GetDashboardAsync(ct));

        [HttpGet("sales")]
        public async Task<IActionResult> Sales(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] Guid? warehouseId,
            CancellationToken ct)
        {
            var fromU = (from ?? DateTime.UtcNow.AddDays(-30)).ToUniversalTime();
            var toU = (to ?? DateTime.UtcNow).ToUniversalTime();
            return Ok(await _service.GetSalesReportAsync(fromU, toU, warehouseId, ct));
        }

        [HttpGet("top-products")]
        public async Task<IActionResult> TopProducts(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int take = 10,
            CancellationToken ct = default)
        {
            var fromU = (from ?? DateTime.UtcNow.AddDays(-30)).ToUniversalTime();
            var toU = (to ?? DateTime.UtcNow).ToUniversalTime();
            return Ok(await _service.GetTopProductsAsync(fromU, toU, take, ct));
        }

        [HttpGet("stock")]
        public async Task<IActionResult> Stock(
            [FromQuery] Guid? warehouseId,
            [FromQuery] bool onlyLow = false,
            CancellationToken ct = default)
            => Ok(await _service.GetStockReportAsync(warehouseId, onlyLow, ct));

        [HttpGet("cash-sessions/{sessionId}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier},{Roles.Accountant}")]
        public async Task<IActionResult> CashSession(Guid sessionId, CancellationToken ct)
            => (await _service.GetCashSessionReportAsync(sessionId, ct)) is { } r ? Ok(r) : NotFound();
    }
}
