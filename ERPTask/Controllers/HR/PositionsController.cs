using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.HR
{
    [ApiController]
    [Route("api/hr/positions")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public class PositionsController : ControllerBase
    {
        private readonly IPositionService _service;
        public PositionsController(IPositionService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _service.GetAllAsync(ct));

        [HttpPost]
        public async Task<IActionResult> Create(CreatePositionDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, ct));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreatePositionDto dto, CancellationToken ct)
            => (await _service.UpdateAsync(id, dto, ct)) is { } p ? Ok(p) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
