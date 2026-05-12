using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Domain.Models.Inventory;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Inventory
{
    public class UnitService : IUnitService
    {
        private readonly ApplicationDbContext _context;

        public UnitService(ApplicationDbContext context) => _context = context;

        public async Task<List<UnitDto>> GetAllAsync()
        {
            return await _context.Units.Select(u => new UnitDto
            {
                Id = u.Id,
                NameAr = u.NameAr,
                NameEn = u.NameEn,
                Code = u.Code,
                IsActive = u.IsActive
            }).ToListAsync();
        }

        public async Task<UnitDto?> GetByIdAsync(Guid id)
        {
            var unit = await _context.Units.FindAsync(id);
            return unit == null ? null : new UnitDto
            {
                Id = unit.Id,
                NameAr = unit.NameAr,
                NameEn = unit.NameEn,
                Code = unit.Code,
                IsActive = unit.IsActive
            };
        }

        public async Task<UnitDto> CreateAsync(CreateUnitDto dto)
        {
            var unit = new Unit
            {
                NameAr = dto.NameAr,
                NameEn = dto.NameEn,
                Code = dto.Code
            };
            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(unit.Id))!;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit == null) return false;
            _context.Units.Remove(unit);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
