using Application.DTOs.POS;
using Application.Inerfaces.POS;
using Domain.Enums;
using Domain.Models.POS;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.POS
{
    public class CashRegisterService : ICashRegisterService
    {
        private readonly ApplicationDbContext _context;

        public CashRegisterService(ApplicationDbContext context) => _context = context;

        public async Task<List<CashRegisterDto>> GetAllAsync()
        {
            return await _context.CashRegisters
                .Include(r => r.Warehouse)
                .Select(r => new CashRegisterDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Code = r.Code,
                    WarehouseId = r.WarehouseId,
                    WarehouseName = r.Warehouse != null ? r.Warehouse.NameAr : null,
                    IsActive = r.IsActive,
                    HasOpenSession = _context.CashSessions
                        .Any(s => s.CashRegisterId == r.Id && s.Status == CashSessionStatus.Open)
                })
                .ToListAsync();
        }

        public async Task<CashRegisterDto> CreateAsync(CreateCashRegisterDto dto)
        {
            var register = new CashRegister
            {
                Name = dto.Name,
                Code = dto.Code,
                WarehouseId = dto.WarehouseId
            };
            _context.CashRegisters.Add(register);
            await _context.SaveChangesAsync();

            return new CashRegisterDto
            {
                Id = register.Id,
                Name = register.Name,
                Code = register.Code,
                WarehouseId = register.WarehouseId,
                IsActive = register.IsActive,
                HasOpenSession = false
            };
        }
    }
}
