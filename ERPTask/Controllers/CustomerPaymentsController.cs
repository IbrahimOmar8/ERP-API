using System.Security.Claims;
using Application.DTOs.Payments;
using Application.Inerfaces.Payments;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/customer-payments")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier},{Roles.Accountant}")]
    public class CustomerPaymentsController : ControllerBase
    {
        private readonly ICustomerPaymentService _service;
        public CustomerPaymentsController(ICustomerPaymentService service) => _service = service;

        private Guid? CurrentUserId =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value, out var id) ? id : null;

        [HttpGet("by-customer/{customerId}")]
        public async Task<IActionResult> ByCustomer(Guid customerId, CancellationToken ct)
            => Ok(await _service.GetByCustomerAsync(customerId, ct));

        [HttpPost]
        public async Task<IActionResult> Record(CreateCustomerPaymentDto dto, CancellationToken ct)
        {
            try { return Ok(await _service.RecordAsync(dto, CurrentUserId, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();

        [HttpGet("ledger/{customerId}")]
        public async Task<IActionResult> Ledger(
            Guid customerId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken ct)
            => (await _service.GetLedgerAsync(customerId, from, to, ct)) is { } l ? Ok(l) : NotFound();
    }
}
