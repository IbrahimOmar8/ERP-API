using Application.DTOs.Production;
using Application.Inerfaces.Production;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.Production
{
    [ApiController]
    [Route("api/production/boms")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public class BomsController : ControllerBase
    {
        private readonly IBomService _service;
        public BomsController(IBomService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? productId, CancellationToken ct)
            => Ok(await _service.GetAllAsync(productId, ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)) is { } b ? Ok(b) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateBomDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, ct));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreateBomDto dto, CancellationToken ct)
            => (await _service.UpdateAsync(id, dto, ct)) is { } b ? Ok(b) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
