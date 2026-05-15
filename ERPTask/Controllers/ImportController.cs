using Application.Inerfaces.Import;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.WarehouseKeeper}")]
    public class ImportController : ControllerBase
    {
        private readonly IImportService _service;
        public ImportController(IImportService service) => _service = service;

        // Multipart form: file=<csv>, dryRun=true|false (default false)
        [HttpPost("products")]
        [RequestSizeLimit(20_000_000)] // 20 MB
        public async Task<IActionResult> Products(
            IFormFile file,
            [FromForm] bool dryRun = false,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "الملف فارغ" });
            await using var stream = file.OpenReadStream();
            return Ok(await _service.ImportProductsAsync(stream, dryRun, ct));
        }

        [HttpPost("customers")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Customers(
            IFormFile file,
            [FromForm] bool dryRun = false,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "الملف فارغ" });
            await using var stream = file.OpenReadStream();
            return Ok(await _service.ImportCustomersAsync(stream, dryRun, ct));
        }

        // Sample templates for users to download
        [HttpGet("template/products")]
        public IActionResult ProductsTemplate()
        {
            const string csv = "sku,barcode,nameAr,nameEn,category,unit,purchasePrice,salePrice,vatRate,minStockLevel\n" +
                               "P-001,1234567890123,صنف تجريبي,Sample Product,عام,قطعة,50,75,14,10\n";
            return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray(),
                "text/csv; charset=utf-8", "products-template.csv");
        }

        [HttpGet("template/customers")]
        public IActionResult CustomersTemplate()
        {
            const string csv = "name,phone,email,address,taxRegistrationNumber,nationalId,isCompany,creditLimit\n" +
                               "عميل تجريبي,01001234567,test@example.com,القاهرة,,,false,0\n";
            return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray(),
                "text/csv; charset=utf-8", "customers-template.csv");
        }
    }
}
