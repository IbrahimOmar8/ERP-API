using Domain.Enums;
using ERPTask.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/sales/{saleId}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier},{Roles.Accountant}")]
    public class InvoicePrintController : ControllerBase
    {
        private readonly InvoicePrintService _service;

        public InvoicePrintController(InvoicePrintService service) => _service = service;

        [HttpGet("print")]
        [Produces("text/html")]
        public async Task<IActionResult> Print(Guid saleId, [FromQuery] string format = "a4")
        {
            var result = await _service.RenderAsync(saleId, thermal80mm: format == "thermal" || format == "80mm");
            return result is null ? NotFound() : Content(result.Value.Html, "text/html; charset=utf-8");
        }

        [HttpGet("qr")]
        [Produces("image/png")]
        public async Task<IActionResult> Qr(Guid saleId)
        {
            var png = await _service.RenderQrOnlyAsync(saleId);
            return png is null ? NotFound() : File(png, "image/png");
        }
    }
}
