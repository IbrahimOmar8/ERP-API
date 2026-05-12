using Application.DTOs.POS;
using Application.Inerfaces.Egypt;
using Application.Inerfaces.POS;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly ISaleService _service;
        private readonly IEInvoiceService _eInvoiceService;

        public SalesController(ISaleService service, IEInvoiceService eInvoiceService)
        {
            _service = service;
            _eInvoiceService = eInvoiceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] SaleFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id) =>
            (await _service.GetByIdAsync(id)) is { } s ? Ok(s) : NotFound();

        [HttpPost("{cashierUserId}")]
        public async Task<IActionResult> Create(Guid cashierUserId, CreateSaleDto dto)
        {
            try { return Ok(await _service.CreateAsync(dto, cashierUserId)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
            => await _service.CancelAsync(id) ? Ok() : NotFound();

        [HttpPost("{id}/refund")]
        public async Task<IActionResult> Refund(Guid id, [FromBody] RefundRequest request) =>
            (await _service.RefundAsync(id, request.Reason, request.UserId)) is { } s
                ? Ok(s) : NotFound();

        [HttpPost("{id}/submit-eta")]
        public async Task<IActionResult> SubmitEta(Guid id)
        {
            try { return Ok(await _eInvoiceService.SubmitSaleAsync(id)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        public record RefundRequest(string? Reason, Guid? UserId);
    }
}
