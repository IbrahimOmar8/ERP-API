using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.WarehouseKeeper},{Roles.Accountant}")]
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
