using System.Security.Claims;
using Application.DTOs.Notifications;
using Application.Inerfaces.Notifications;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _service;
        public NotificationsController(INotificationService service) => _service = service;

        private Guid? CurrentUserId =>
            Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value, out var id) ? id : null;

        [HttpGet]
        public async Task<IActionResult> Mine(
            [FromQuery] bool unreadOnly = false,
            [FromQuery] int take = 30,
            CancellationToken ct = default)
        {
            if (CurrentUserId is not { } id) return Unauthorized();
            return Ok(await _service.GetForUserAsync(id, unreadOnly, take, ct));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount(CancellationToken ct)
        {
            if (CurrentUserId is not { } id) return Unauthorized();
            return Ok(new { count = await _service.GetUnreadCountAsync(id, ct) });
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return await _service.MarkReadAsync(id, userId, ct) ? NoContent() : NotFound();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead(CancellationToken ct)
        {
            if (CurrentUserId is not { } userId) return Unauthorized();
            return Ok(new { marked = await _service.MarkAllReadAsync(userId, ct) });
        }

        // Admins can broadcast a manual notification (e.g. system maintenance)
        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
        public async Task<IActionResult> Create(CreateNotificationDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, ct));
    }
}
