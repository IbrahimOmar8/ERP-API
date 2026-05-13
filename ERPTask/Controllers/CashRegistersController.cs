using Application.DTOs.POS;
using Application.Inerfaces.POS;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Manager},{Roles.Cashier}")]
    public class CashRegistersController : ControllerBase
    {
        private readonly ICashRegisterService _service;
        public CashRegistersController(ICashRegisterService service) => _service = service;

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpPost]
        public async Task<IActionResult> Create(CreateCashRegisterDto dto)
            => Ok(await _service.CreateAsync(dto));
    }
}
