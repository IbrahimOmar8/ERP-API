using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseInvoicesController : ControllerBase
    {
        private readonly IPurchaseInvoiceService _service;
        public PurchaseInvoicesController(IPurchaseInvoiceService service) => _service = service;

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id) =>
            (await _service.GetByIdAsync(id)) is { } i ? Ok(i) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreatePurchaseInvoiceDto dto)
            => Ok(await _service.CreateAsync(dto, null));
    }
}
