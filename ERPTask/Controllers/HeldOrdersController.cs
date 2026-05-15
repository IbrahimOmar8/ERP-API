using System.Security.Claims;
using Application.DTOs.POS;
using Application.Inerfaces.POS;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/held-orders")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier}")]
    public class HeldOrdersController : ControllerBase
    {
        private readonly IHeldOrderService _service;
        public HeldOrdersController(IHeldOrderService service) => _service = service;

        private Guid CurrentUserId
        {
            get
            {
                var c = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
                return Guid.TryParse(c, out var id) ? id : Guid.Empty;
            }
        }

        private bool IsManagerOrAdmin =>
            User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Manager);

        // Cashiers see their own. Admin/Manager see all (or filter by cashier).
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? cashier, CancellationToken ct)
        {
            Guid? filter = IsManagerOrAdmin ? cashier : CurrentUserId;
            return Ok(await _service.GetAllAsync(filter, ct));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        {
            var order = await _service.GetByIdAsync(id, ct);
            if (order == null) return NotFound();
            if (!IsManagerOrAdmin && order.CashierUserId != CurrentUserId) return Forbid();
            return Ok(order);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateHeldOrderDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, CurrentUserId, ct));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var order = await _service.GetByIdAsync(id, ct);
            if (order == null) return NotFound();
            if (!IsManagerOrAdmin && order.CashierUserId != CurrentUserId) return Forbid();
            return await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
        }
    }
}
