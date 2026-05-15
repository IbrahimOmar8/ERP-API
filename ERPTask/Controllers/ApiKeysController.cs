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
    [Route("api/api-keys")]
    // API keys are managed by humans only — never via another API key
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public class ApiKeysController : ControllerBase
    {
        private readonly IApiKeyService _service;
        public ApiKeysController(IApiKeyService service) => _service = service;

        private Guid? CurrentUserId =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value, out var id) ? id : null;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _service.GetAllAsync(ct));

        // Returns the raw key in the response — show it to the user once and never again.
        [HttpPost]
        public async Task<IActionResult> Create(CreateApiKeyDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, CurrentUserId, ct));

        [HttpPost("{id}/revoke")]
        public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
            => await _service.RevokeAsync(id, ct) ? NoContent() : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
