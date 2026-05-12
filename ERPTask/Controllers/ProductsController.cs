using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;
        public ProductsController(IProductService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] ProductFilterDto filter)
            => Ok(await _service.GetAllAsync(filter));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id) =>
            (await _service.GetByIdAsync(id)) is { } p ? Ok(p) : NotFound();

        [HttpGet("barcode/{barcode}")]
        public async Task<IActionResult> GetByBarcode(string barcode) =>
            (await _service.GetByBarcodeAsync(barcode)) is { } p ? Ok(p) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductDto dto)
            => Ok(await _service.CreateAsync(dto));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateProductDto dto) =>
            (await _service.UpdateAsync(id, dto)) is { } p ? Ok(p) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
