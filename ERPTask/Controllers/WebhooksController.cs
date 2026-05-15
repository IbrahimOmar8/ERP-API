using System.Security.Claims;
using Application.DTOs.Integration;
using Application.Inerfaces.Integration;
using Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
               Roles = $"{Roles.Admin},{Roles.Manager}")]
    public class WebhooksController : ControllerBase
    {
        private readonly IWebhookService _service;
        public WebhooksController(IWebhookService service) => _service = service;

        private Guid? CurrentUserId =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value, out var id) ? id : null;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await _service.GetAllAsync(ct));

        [HttpPost]
        public async Task<IActionResult> Create(CreateWebhookDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, CurrentUserId, ct));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, CreateWebhookDto dto, CancellationToken ct)
            => (await _service.UpdateAsync(id, dto, ct)) is { } w ? Ok(w) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();

        [HttpGet("{id}/deliveries")]
        public async Task<IActionResult> Deliveries(Guid id, [FromQuery] int take = 50, CancellationToken ct = default)
            => Ok(await _service.GetDeliveriesAsync(id, take, ct));

        // Lists known events so admin can pick from a dropdown
        [HttpGet("events")]
        public IActionResult Events() => Ok(WebhookEvents.All);

        public record TestRequest(Guid SubscriptionId);

        [HttpPost("test")]
        public async Task<IActionResult> Test(TestRequest req, CancellationToken ct)
        {
            // Convenience: send a synthetic event to all matching webhooks of a given subscription.
            // We use a stable event name "test.ping" so any subscription configured for "test.*" or "*"
            // (or explicitly "test.ping") will receive it.
            await _service.DispatchAsync("test.ping", new { subscriptionId = req.SubscriptionId, at = DateTime.UtcNow }, ct);
            return Ok(new { sent = true });
        }
    }
}
