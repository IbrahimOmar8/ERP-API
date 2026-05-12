using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Domain.Models.Inventory;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Inventory
{
    public class WarehouseService : IWarehouseService
    {
        private readonly ApplicationDbContext _context;

        public WarehouseService(ApplicationDbContext context) => _context = context;

        public async Task<List<WarehouseDto>> GetAllAsync()
        {
            return await _context.Warehouses.Select(w => new WarehouseDto
            {
                Id = w.Id,
                NameAr = w.NameAr,
                NameEn = w.NameEn,
                Code = w.Code,
                Address = w.Address,
                Phone = w.Phone,
                IsMain = w.IsMain,
                IsActive = w.IsActive
            }).ToListAsync();
        }

        public async Task<WarehouseDto?> GetByIdAsync(Guid id)
        {
            var w = await _context.Warehouses.FindAsync(id);
            return w == null ? null : Map(w);
        }

        public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto dto)
        {
            var w = new Warehouse
            {
                NameAr = dto.NameAr,
                NameEn = dto.NameEn,
                Code = dto.Code,
                Address = dto.Address,
                Phone = dto.Phone,
                ManagerEmployeeId = dto.ManagerEmployeeId,
                IsMain = dto.IsMain
            };

            _context.Warehouses.Add(w);
            await _context.SaveChangesAsync();
            return Map(w);
        }

        public async Task<WarehouseDto?> UpdateAsync(Guid id, CreateWarehouseDto dto)
        {
            var w = await _context.Warehouses.FindAsync(id);
            if (w == null) return null;

            w.NameAr = dto.NameAr;
            w.NameEn = dto.NameEn;
            w.Code = dto.Code;
            w.Address = dto.Address;
            w.Phone = dto.Phone;
            w.ManagerEmployeeId = dto.ManagerEmployeeId;
            w.IsMain = dto.IsMain;

            await _context.SaveChangesAsync();
            return Map(w);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var w = await _context.Warehouses.FindAsync(id);
            if (w == null) return false;
            _context.Warehouses.Remove(w);
            await _context.SaveChangesAsync();
            return true;
        }

        private static WarehouseDto Map(Warehouse w) => new()
        {
            Id = w.Id,
            NameAr = w.NameAr,
            NameEn = w.NameEn,
            Code = w.Code,
            Address = w.Address,
            Phone = w.Phone,
            IsMain = w.IsMain,
            IsActive = w.IsActive
        };
    }
}
