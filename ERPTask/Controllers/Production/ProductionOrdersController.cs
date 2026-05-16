using System.Security.Claims;
using Application.DTOs.Production;
using Application.Inerfaces.Production;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.Production
{
    [ApiController]
    [Route("api/production/orders")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public class ProductionOrdersController : ControllerBase
    {
        private readonly IProductionOrderService _service;
        public ProductionOrdersController(IProductionOrderService service) => _service = service;

        private Guid? CurrentUserId
        {
            get
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
                return Guid.TryParse(claim, out var id) ? id : (Guid?)null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _service.GetAllAsync(ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)) is { } o ? Ok(o) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductionOrderDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, CurrentUserId, ct));

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
            => (await _service.CompleteAsync(id, CurrentUserId, ct)) is { } o ? Ok(o) : NotFound();

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
            => (await _service.CancelAsync(id, ct)) is { } o ? Ok(o) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
