using Application.DTOs.Egypt;
using Application.Inerfaces.Egypt;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/company-profile")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager}")]
    public class CompanyProfileController : ControllerBase
    {
        private readonly ICompanyProfileService _service;
        public CompanyProfileController(ICompanyProfileService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct)
            => Ok(await _service.GetAsync(ct));

        [HttpPut]
        public async Task<IActionResult> Upsert(UpdateCompanyProfileDto dto, CancellationToken ct)
            => Ok(await _service.UpsertAsync(dto, ct));
    }
}
