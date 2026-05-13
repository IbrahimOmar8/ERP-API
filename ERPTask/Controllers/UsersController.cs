using Application.DTOs.Auth;
using Application.Inerfaces.Auth;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = Roles.Admin)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;
        public UsersController(IUserService service) => _service = service;

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id) =>
            (await _service.GetByIdAsync(id)) is { } u ? Ok(u) : NotFound();

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, RegisterDto dto) =>
            (await _service.UpdateAsync(id, dto)) is { } u ? Ok(u) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
            => await _service.DeleteAsync(id) ? NoContent() : NotFound();

        [HttpPost("{id}/active/{active:bool}")]
        public async Task<IActionResult> SetActive(Guid id, bool active)
            => await _service.SetActiveAsync(id, active) ? NoContent() : NotFound();
    }
}
