using System.Security.Claims;
using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.HR
{
    [ApiController]
    [Route("api/hr/leaves")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public class LeavesController : ControllerBase
    {
        private readonly ILeaveRequestService _service;
        public LeavesController(ILeaveRequestService service) => _service = service;

        private Guid? CurrentUserId
        {
            get
            {
                var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value;
                return Guid.TryParse(claim, out var id) ? id : (Guid?)null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? employeeId, [FromQuery] LeaveStatus? status, CancellationToken ct)
            => Ok(await _service.GetAllAsync(employeeId, status, ct));

        [HttpPost]
        public async Task<IActionResult> Create(CreateLeaveRequestDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, ct));

        [HttpPost("{id}/approve")]
        public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
            => (await _service.SetStatusAsync(id, LeaveStatus.Approved, CurrentUserId, ct)) is { } r ? Ok(r) : NotFound();

        [HttpPost("{id}/reject")]
        public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
            => (await _service.SetStatusAsync(id, LeaveStatus.Rejected, CurrentUserId, ct)) is { } r ? Ok(r) : NotFound();

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
            => (await _service.SetStatusAsync(id, LeaveStatus.Cancelled, CurrentUserId, ct)) is { } r ? Ok(r) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
