using Application.DTOs;
using Application.Inerfaces;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _service;
        public EmployeesController(IEmployeeService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] FilterEmployeeDto filter)
            => Ok(await _service.GetEmployeesAsync(filter));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id) =>
            (await _service.GetEmployeeByIdAsync(id)) is { } e ? Ok(e) : NotFound();

        [HttpPost]
        public async Task<IActionResult> Create(CreateEmployeeDto dto)
            => Ok(await _service.CreateEmployeeAsync(dto));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UpdateEmployeeDto dto) =>
            (await _service.UpdateEmployeeAsync(id, dto)) is { } e ? Ok(e) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
            => await _service.DeleteEmployeeAsync(id) ? NoContent() : NotFound();
    }
}
