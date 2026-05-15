using Application.DTOs.Loyalty;
using Application.Inerfaces.Loyalty;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier}")]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponService _service;
        public CouponsController(ICouponService service) => _service = service;

        [HttpGet] public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id, CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)) is { } c ? Ok(c) : NotFound();

        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Create(CreateCouponDto dto, CancellationToken ct)
        {
            try { return Ok(await _service.CreateAsync(dto, ct)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Update(Guid id, CreateCouponDto dto, CancellationToken ct)
        {
            try { return (await _service.UpdateAsync(id, dto, ct)) is { } c ? Ok(c) : NotFound(); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();

        // Cashier-facing — checks code validity & returns discount amount
        [HttpPost("validate")]
        public async Task<IActionResult> Validate(ValidateCouponRequest request, CancellationToken ct)
            => Ok(await _service.ValidateAsync(request, ct));
    }
}
