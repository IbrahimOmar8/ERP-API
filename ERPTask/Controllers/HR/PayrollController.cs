using System.Security.Claims;
using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.HR
{
    [ApiController]
    [Route("api/hr/payroll")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Accountant}")]
    public class PayrollController : ControllerBase
    {
        private readonly IPayrollService _service;
        public PayrollController(IPayrollService service) => _service = service;

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
        public async Task<IActionResult> GetForPeriod([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
            => Ok(await _service.GetForPeriodAsync(year, month, ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)) is { } p ? Ok(p) : NotFound();

        [HttpPost("generate")]
        public async Task<IActionResult> Generate(GeneratePayrollDto dto, CancellationToken ct)
            => Ok(await _service.GenerateAsync(dto, ct));

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
            => (await _service.SetStatusAsync(id, PayrollStatus.Approved, CurrentUserId, ct)) is { } p ? Ok(p) : NotFound();

        [HttpPost("{id}/pay")]
        public async Task<IActionResult> Pay(Guid id, CancellationToken ct)
            => (await _service.SetStatusAsync(id, PayrollStatus.Paid, CurrentUserId, ct)) is { } p ? Ok(p) : NotFound();

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
            => (await _service.SetStatusAsync(id, PayrollStatus.Cancelled, CurrentUserId, ct)) is { } p ? Ok(p) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
