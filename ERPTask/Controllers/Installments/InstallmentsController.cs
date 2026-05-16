using System.Security.Claims;
using Application.DTOs.Installments;
using Application.Inerfaces.Installments;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.Installments
{
    [ApiController]
    [Route("api/installments")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Accountant}")]
    public class InstallmentsController : ControllerBase
    {
        private readonly IInstallmentService _service;
        public InstallmentsController(IInstallmentService service) => _service = service;

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
        public async Task<IActionResult> GetAll(
            [FromQuery] Guid? customerId,
            [FromQuery] InstallmentPlanStatus? status,
            CancellationToken ct)
            => Ok(await _service.GetAllAsync(customerId, status, ct));

        [HttpGet("overdue")]
        public async Task<IActionResult> Overdue(CancellationToken ct)
            => Ok(await _service.GetOverdueAsync(ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)) is { } p ? Ok(p) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateInstallmentPlanDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, CurrentUserId, ct));

        [HttpPost("installment/{installmentId}/pay")]
        public async Task<IActionResult> Pay(Guid installmentId, PayInstallmentDto dto, CancellationToken ct)
            => (await _service.PayInstallmentAsync(installmentId, dto, CurrentUserId, ct)) is { } p ? Ok(p) : NotFound();

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
            => (await _service.CancelAsync(id, ct)) is { } p ? Ok(p) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
