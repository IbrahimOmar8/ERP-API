using System.Security.Claims;
using Application.DTOs.Payments;
using Application.Inerfaces.Payments;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/supplier-payments")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Accountant}")]
    public class SupplierPaymentsController : ControllerBase
    {
        private readonly ISupplierPaymentService _service;
        public SupplierPaymentsController(ISupplierPaymentService service) => _service = service;

        private Guid? CurrentUserId =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value, out var id) ? id : null;

        [HttpGet("by-supplier/{supplierId}")]
        public async Task<IActionResult> BySupplier(Guid supplierId, CancellationToken ct)
            => Ok(await _service.GetBySupplierAsync(supplierId, ct));

        [HttpPost]
        public async Task<IActionResult> Record(CreateSupplierPaymentDto dto, CancellationToken ct)
        {
            try { return Ok(await _service.RecordAsync(dto, CurrentUserId, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();

        [HttpGet("ledger/{supplierId}")]
        public async Task<IActionResult> Ledger(
            Guid supplierId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken ct)
            => (await _service.GetLedgerAsync(supplierId, from, to, ct)) is { } l ? Ok(l) : NotFound();
    }
}
