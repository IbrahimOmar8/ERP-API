using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UnitsController : ControllerBase
    {
        private readonly IUnitService _service;
        public UnitsController(IUnitService service) => _service = service;

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id) =>
            (await _service.GetByIdAsync(id)) is { } u ? Ok(u) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateUnitDto dto)
            => Ok(await _service.CreateAsync(dto));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
