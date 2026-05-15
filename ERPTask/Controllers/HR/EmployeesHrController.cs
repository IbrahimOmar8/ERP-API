using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.HR
{
    [ApiController]
    [Route("api/hr/employees")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public class EmployeesHrController : ControllerBase
    {
        private readonly IEmployeeHrService _service;
        public EmployeesHrController(IEmployeeHrService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] EmpStatus? status, CancellationToken ct)
            => Ok(await _service.GetAllAsync(status, ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)) is { } e ? Ok(e) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateEmployeeFullDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, ct));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreateEmployeeFullDto dto, CancellationToken ct)
            => (await _service.UpdateAsync(id, dto, ct)) is { } e ? Ok(e) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
