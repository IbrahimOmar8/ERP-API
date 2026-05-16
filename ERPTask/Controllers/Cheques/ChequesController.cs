using System.Security.Claims;
using Application.DTOs.Cheques;
using Application.Inerfaces.Cheques;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.Cheques
{
    [ApiController]
    [Route("api/cheques")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Accountant}")]
    public class ChequesController : ControllerBase
    {
        private readonly IChequeService _service;
        public ChequesController(IChequeService service) => _service = service;

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
        public async Task<IActionResult> Get([FromQuery] ChequeFilterDto filter, CancellationToken ct)
            => Ok(await _service.GetAsync(filter, ct));

        [HttpGet("stats")]
        public async Task<IActionResult> Stats(CancellationToken ct)
            => Ok(await _service.GetStatsAsync(ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)) is { } c ? Ok(c) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateChequeDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, CurrentUserId, ct));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreateChequeDto dto, CancellationToken ct)
            => (await _service.UpdateAsync(id, dto, ct)) is { } c ? Ok(c) : NotFound();

        [HttpPost("{id}/deposit")]
        public async Task<IActionResult> Deposit(Guid id, CancellationToken ct)
            => (await _service.DepositAsync(id, ct)) is { } c ? Ok(c) : NotFound();

        [HttpPost("{id}/clear")]
        public async Task<IActionResult> Clear(Guid id, CancellationToken ct)
            => (await _service.ClearAsync(id, CurrentUserId, ct)) is { } c ? Ok(c) : NotFound();

        [HttpPost("{id}/bounce")]
        public async Task<IActionResult> Bounce(Guid id, BounceChequeDto dto, CancellationToken ct)
            => (await _service.BounceAsync(id, dto, ct)) is { } c ? Ok(c) : NotFound();

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
            => (await _service.CancelAsync(id, ct)) is { } c ? Ok(c) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
