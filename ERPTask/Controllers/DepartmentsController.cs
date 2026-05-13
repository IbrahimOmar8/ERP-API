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
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentService _service;
        public DepartmentsController(IDepartmentService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
            => Ok(await _service.GetListAsync(pageNumber, pageSize));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id) => Ok(await _service.GetByIdAsync(id));

        [HttpPost]
        public async Task<IActionResult> Create(CreateDepartmentDto dto)
            => Ok(await _service.CreateAsync(dto));
    }
}
