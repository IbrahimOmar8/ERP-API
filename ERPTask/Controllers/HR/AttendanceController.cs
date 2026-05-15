using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.HR
{
    [ApiController]
    [Route("api/hr/attendance")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public class AttendanceController : ControllerBase
    {
        private readonly IAttendanceService _service;
        public AttendanceController(IAttendanceService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] AttendanceFilterDto filter, CancellationToken ct)
            => Ok(await _service.GetAsync(filter, ct));

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
            => Ok(await _service.GetSummaryAsync(from, to, ct));

        [HttpPost("check-in")]
        public async Task<IActionResult> CheckIn(CheckInDto dto, CancellationToken ct)
            => Ok(await _service.CheckInAsync(dto, ct));

        [HttpPost("check-out")]
        public async Task<IActionResult> CheckOut(CheckOutDto dto, CancellationToken ct)
            => (await _service.CheckOutAsync(dto, ct)) is { } a ? Ok(a) : NotFound();

        [HttpPost("manual")]
        public async Task<IActionResult> UpsertManual(ManualAttendanceDto dto, CancellationToken ct)
            => Ok(await _service.UpsertManualAsync(dto, ct));

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
