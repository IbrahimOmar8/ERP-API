using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.HR
{
    [ApiController]
    [Route("api/hr/shifts")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public class ShiftsController : ControllerBase
    {
        private readonly IShiftService _service;
        public ShiftsController(IShiftService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _service.GetAllAsync(ct));

        [HttpPost]
        public async Task<IActionResult> Create(CreateShiftDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, ct));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreateShiftDto dto, CancellationToken ct)
            => (await _service.UpdateAsync(id, dto, ct)) is { } s ? Ok(s) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();

        [HttpGet("assignments")]
        public async Task<IActionResult> GetAssignments([FromQuery] Guid? employeeId, CancellationToken ct)
            => Ok(await _service.GetAssignmentsAsync(employeeId, ct));

        [HttpPost("assignments")]
        public async Task<IActionResult> Assign(CreateShiftAssignmentDto dto, CancellationToken ct)
            => Ok(await _service.AssignAsync(dto, ct));

        [HttpDelete("assignments/{id}")]
        public async Task<IActionResult> RemoveAssignment(Guid id, CancellationToken ct)
            => await _service.RemoveAssignmentAsync(id, ct) ? NoContent() : NotFound();
    }
}
