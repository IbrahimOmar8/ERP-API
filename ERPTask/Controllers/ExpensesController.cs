using System.Security.Claims;
using Application.DTOs.Accounting;
using Application.Inerfaces.Accounting;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Accountant}")]
    public class ExpensesController : ControllerBase
    {
        private readonly IExpenseService _service;
        public ExpensesController(IExpenseService service) => _service = service;

        private Guid? CurrentUserId =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value, out var id) ? id : null;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ExpenseFilterDto filter, CancellationToken ct)
            => Ok(await _service.GetAllAsync(filter, ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)) is { } e ? Ok(e) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateExpenseDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, CurrentUserId, ct));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreateExpenseDto dto, CancellationToken ct)
            => (await _service.UpdateAsync(id, dto, ct)) is { } e ? Ok(e) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();

        [HttpGet("summary")]
        public async Task<IActionResult> Summary(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            CancellationToken ct)
        {
            var f = (from ?? DateTime.UtcNow.AddDays(-30)).ToUniversalTime();
            var t = (to ?? DateTime.UtcNow).ToUniversalTime();
            return Ok(await _service.GetSummaryAsync(f, t, ct));
        }
    }
}
