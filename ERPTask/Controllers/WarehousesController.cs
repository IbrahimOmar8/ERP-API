using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseService _service;
        public WarehousesController(IWarehouseService service) => _service = service;

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id) =>
            (await _service.GetByIdAsync(id)) is { } w ? Ok(w) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateWarehouseDto dto)
            => Ok(await _service.CreateAsync(dto));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreateWarehouseDto dto) =>
            (await _service.UpdateAsync(id, dto)) is { } w ? Ok(w) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
