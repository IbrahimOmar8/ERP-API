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
            return await ReloadAsync(register.Id);
        }

        public async Task<CashRegisterDto?> UpdateAsync(Guid id, CreateCashRegisterDto dto)
        {
            var register = await _context.CashRegisters.FindAsync(id);
            if (register == null) return null;
            register.Name = dto.Name;
            register.Code = dto.Code;
            register.WarehouseId = dto.WarehouseId;
            await _context.SaveChangesAsync();
            return await ReloadAsync(id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var register = await _context.CashRegisters.FindAsync(id);
            if (register == null) return false;
            if (await _context.CashSessions.AnyAsync(s => s.CashRegisterId == id))
                throw new InvalidOperationException("لا يمكن حذف ماكينة لها جلسات");
            _context.CashRegisters.Remove(register);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CashRegisterDto?> SetActiveAsync(Guid id, bool active)
        {
            var register = await _context.CashRegisters.FindAsync(id);
            if (register == null) return null;
            register.IsActive = active;
            await _context.SaveChangesAsync();
            return await ReloadAsync(id);
        }

        private async Task<CashRegisterDto> ReloadAsync(Guid id) =>
            (await GetAllAsync()).First(r => r.Id == id);
    }
}
