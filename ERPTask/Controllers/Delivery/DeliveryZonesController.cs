using Application.DTOs.Delivery;
using Application.Inerfaces.Delivery;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.Delivery
{
    [ApiController]
    [Route("api/delivery/zones")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier}")]
    public class DeliveryZonesController : ControllerBase
    {
        private readonly IDeliveryZoneService _service;
        public DeliveryZonesController(IDeliveryZoneService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _service.GetAllAsync(ct));

        [HttpPost]
        public async Task<IActionResult> Create(CreateZoneDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, ct));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreateZoneDto dto, CancellationToken ct)
            => (await _service.UpdateAsync(id, dto, ct)) is { } z ? Ok(z) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
