using Application.DTOs.POS;
using Application.Inerfaces.POS;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier}")]
    public class CashRegistersController : ControllerBase
    {
        private readonly ICashRegisterService _service;
        public CashRegistersController(ICashRegisterService service) => _service = service;

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Create(CreateCashRegisterDto dto)
            => Ok(await _service.CreateAsync(dto));

        [HttpPut("{id}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Update(Guid id, CreateCashRegisterDto dto)
            => (await _service.UpdateAsync(id, dto)) is { } r ? Ok(r) : NotFound();

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try { return await _service.DeleteAsync(id) ? NoContent() : NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("{id}/active/{active:bool}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> SetActive(Guid id, bool active)
            => (await _service.SetActiveAsync(id, active)) is { } r ? Ok(r) : NotFound();
    }
}
