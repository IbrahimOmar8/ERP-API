using Application.Inerfaces.Egypt;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/einvoice")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Accountant}")]
    public class EInvoiceController : ControllerBase
    {
        private readonly IEInvoiceService _service;

        public EInvoiceController(IEInvoiceService service) => _service = service;

        public record SubmitRequest(string? SignedCmsBase64);
        public record CancelRequest(string Reason);

        [HttpPost("sales/{saleId}/submit")]
        public async Task<IActionResult> Submit(Guid saleId, [FromBody] SubmitRequest? body, CancellationToken ct)
        {
            try { return Ok(await _service.SubmitSaleAsync(saleId, body?.SignedCmsBase64, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("sales/{saleId}/refresh")]
        public async Task<IActionResult> Refresh(Guid saleId, CancellationToken ct)
        {
            try { return Ok(await _service.RefreshStatusAsync(saleId, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("sales/{saleId}/cancel")]
        public async Task<IActionResult> Cancel(Guid saleId, [FromBody] CancelRequest body, CancellationToken ct)
        {
            try { return Ok(await _service.CancelAsync(saleId, body.Reason, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpGet("sales/{saleId}")]
        public async Task<IActionResult> Get(Guid saleId)
            => (await _service.GetSubmissionAsync(saleId)) is { } s ? Ok(s) : NotFound();

        [HttpGet("recent")]
        public async Task<IActionResult> Recent([FromQuery] int take = 50)
            => Ok(await _service.GetRecentAsync(take));
    }
}
