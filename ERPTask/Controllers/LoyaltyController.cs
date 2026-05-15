using System.Security.Claims;
using Application.DTOs.Loyalty;
using Application.Inerfaces.Loyalty;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyService _service;
        public LoyaltyController(ILoyaltyService service) => _service = service;

        private Guid? CurrentUserId =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value, out var id) ? id : null;

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings(CancellationToken ct)
            => Ok(await _service.GetSettingsAsync(ct));

        [HttpPut("settings")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> UpdateSettings(LoyaltySettingsDto dto, CancellationToken ct)
            => Ok(await _service.UpdateSettingsAsync(dto, ct));

        [HttpGet("customers/{id}")]
        public async Task<IActionResult> CustomerStatus(Guid id, CancellationToken ct)
            => (await _service.GetCustomerStatusAsync(id, ct)) is { } s ? Ok(s) : NotFound();

        public record AdjustPointsRequest(int Delta, string? Notes);

        [HttpPost("customers/{id}/adjust")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Adjust(Guid id, AdjustPointsRequest request, CancellationToken ct)
        {
            try
            {
                var balance = await _service.AdjustPointsAsync(id, request.Delta, request.Notes, CurrentUserId, ct);
                return Ok(new { customerId = id, balance });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }
    }
}
