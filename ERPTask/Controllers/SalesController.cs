using System.Security.Claims;
using Application.DTOs.POS;
using Application.Inerfaces.Egypt;
using Application.Inerfaces.POS;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier},{Roles.Accountant}")]
    public class SalesController : ControllerBase
    {
        private readonly ISaleService _service;
        private readonly IEInvoiceService _eInvoiceService;

        public SalesController(ISaleService service, IEInvoiceService eInvoiceService)
        {
            _service = service;
            _eInvoiceService = eInvoiceService;
        }

        private Guid CurrentUserId
        {
            get
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
                return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] SaleFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id) =>
            (await _service.GetByIdAsync(id)) is { } s ? Ok(s) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateSaleDto dto)
        {
            try { return Ok(await _service.CreateAsync(dto, CurrentUserId)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Cancel(Guid id)
            => await _service.CancelAsync(id) ? Ok() : NotFound();

        [HttpPost("{id}/refund")]
        public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest request) =>
            (await _service.RefundAsync(id, request.Reason, CurrentUserId)) is { } s
                ? Ok(s) : NotFound();

        [HttpPost("{id}/submit-eta")]
        public async Task<IActionResult> SubmitEta(Guid id)
        {
            try { return Ok(await _eInvoiceService.SubmitSaleAsync(id)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        public record RefundRequest(string? Reason);
    }
}
