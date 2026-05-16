using System.Security.Claims;
using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers.HR
{
    [ApiController]
    [Route("api/hr/loans")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Accountant}")]
    public class EmployeeLoansController : ControllerBase
    {
        private readonly IEmployeeLoanService _service;
        public EmployeeLoansController(IEmployeeLoanService service) => _service = service;

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
        public async Task<IActionResult> GetAll([FromQuery] Guid? employeeId, [FromQuery] EmployeeLoanStatus? status, CancellationToken ct)
            => Ok(await _service.GetAllAsync(employeeId, status, ct));

        [HttpPost]
        public async Task<IActionResult> Create(CreateEmployeeLoanDto dto, CancellationToken ct)
            => Ok(await _service.CreateAsync(dto, CurrentUserId, ct));

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
            => (await _service.CancelAsync(id, ct)) is { } l ? Ok(l) : NotFound();

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
            => await _service.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }
}
