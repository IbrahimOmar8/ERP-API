using System.Security.Claims;
using Application.DTOs.Delivery;
using Application.Inerfaces.Delivery;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.Delivery
{
    [ApiController]
    [Route("api/delivery/orders")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier}")]
    public class DeliveryOrdersController : ControllerBase
    {
        private readonly IDeliveryOrderService _service;
        public DeliveryOrdersController(IDeliveryOrderService service) => _service = service;

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
        public async Task<IActionResult> Get([FromQuery] DeliveryFilterDto filter, CancellationToken ct)
            => Ok(await _service.GetAsync(filter, ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)) is { } o ? Ok(o) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateDeliveryOrderDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, CurrentUserId, ct));

        [HttpPost("{id}/assign")]
        public async Task<IActionResult> Assign(Guid id, AssignDriverDto dto, CancellationToken ct)
            => (await _service.AssignAsync(id, dto.DriverId, ct)) is { } o ? Ok(o) : NotFound();

        [HttpPost("{id}/pickup")]
        public async Task<IActionResult> PickUp(Guid id, CancellationToken ct)
            => (await _service.PickUpAsync(id, ct)) is { } o ? Ok(o) : NotFound();

        [HttpPost("{id}/deliver")]
        public async Task<IActionResult> Deliver(Guid id, DeliverDto dto, CancellationToken ct)
            => (await _service.DeliverAsync(id, dto, CurrentUserId, ct)) is { } o ? Ok(o) : NotFound();

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
            => (await _service.CancelAsync(id, ct)) is { } o ? Ok(o) : NotFound();

        [HttpPost("{id}/return")]
        public async Task<IActionResult> Return(Guid id, CancellationToken ct)
            => (await _service.ReturnAsync(id, ct)) is { } o ? Ok(o) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();

        [HttpGet("/api/delivery/reconciliation")]
        public async Task<IActionResult> Reconciliation([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        {
            var fromU = (from ?? DateTime.UtcNow.AddDays(-7)).ToUniversalTime();
            var toU = (to ?? DateTime.UtcNow).ToUniversalTime();
            return Ok(await _service.GetReconciliationAsync(fromU, toU, ct));
        }
    }
}
