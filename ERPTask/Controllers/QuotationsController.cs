using System.Security.Claims;
using Application.DTOs.POS;
using Application.Inerfaces.POS;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier},{Roles.Accountant}")]
    public class QuotationsController : ControllerBase
    {
        private readonly IQuotationService _service;
        public QuotationsController(IQuotationService service) => _service = service;

        private Guid CurrentUserId
        {
            get
            {
                var c = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;
                return Guid.TryParse(c, out var id) ? id : Guid.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] QuotationFilterDto filter, CancellationToken ct)
            => Ok(await _service.GetAllAsync(filter, ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)) is { } q ? Ok(q) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateQuotationDto dto, CancellationToken ct)
        {
            try { return Ok(await _service.CreateAsync(dto, CurrentUserId, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreateQuotationDto dto, CancellationToken ct)
        {
            try { return (await _service.UpdateAsync(id, dto, ct)) is { } q ? Ok(q) : NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        public record SetStatusRequest(QuotationStatus Status);

        [HttpPost("{id}/status")]
        public async Task<IActionResult> SetStatus(Guid id, SetStatusRequest req, CancellationToken ct)
        {
            try { return (await _service.SetStatusAsync(id, req.Status, ct)) is { } q ? Ok(q) : NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            try { return await _service.DeleteAsync(id, ct) ? NoContent() : NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("{id}/convert")]
        public async Task<IActionResult> Convert(Guid id, ConvertQuotationDto dto, CancellationToken ct)
        {
            try
            {
                var saleId = await _service.ConvertToSaleAsync(id, dto, CurrentUserId, ct);
                return Ok(new { saleId });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }
    }
}
