using Application.DTOs.Delivery;
using Application.Inerfaces.Delivery;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.Delivery
{
    [ApiController]
    [Route("api/delivery/drivers")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier}")]
    public class DriversController : ControllerBase
    {
        private readonly IDriverService _service;
        public DriversController(IDriverService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool? activeOnly, CancellationToken ct)
            => Ok(await _service.GetAllAsync(activeOnly, ct));

        [HttpPost]
        public async Task<IActionResult> Create(CreateDriverDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, ct));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreateDriverDto dto, CancellationToken ct)
            => (await _service.UpdateAsync(id, dto, ct)) is { } d ? Ok(d) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
