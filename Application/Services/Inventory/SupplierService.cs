using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Domain.Models.Inventory;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Inventory
{
    public class SupplierService : ISupplierService
    {
        private readonly ApplicationDbContext _context;

        public SupplierService(ApplicationDbContext context) => _context = context;

        public async Task<List<SupplierDto>> GetAllAsync()
        {
            return await _context.Suppliers.Select(s => new SupplierDto
            {
                Id = s.Id,
                Name = s.Name,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                TaxRegistrationNumber = s.TaxRegistrationNumber,
                CommercialRegister = s.CommercialRegister,
                Balance = s.Balance,
                IsActive = s.IsActive
            }).ToListAsync();
        }

        public async Task<SupplierDto?> GetByIdAsync(Guid id)
        {
            var s = await _context.Suppliers.FindAsync(id);
            return s == null ? null : Map(s);
        }

        public async Task<SupplierDto> CreateAsync(CreateSupplierDto dto)
        {
            var s = new Supplier
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                TaxRegistrationNumber = dto.TaxRegistrationNumber,
                CommercialRegister = dto.CommercialRegister
            };
            _context.Suppliers.Add(s);
            await _context.SaveChangesAsync();
            return Map(s);
        }

        public async Task<SupplierDto?> UpdateAsync(Guid id, CreateSupplierDto dto)
        {
            var s = await _context.Suppliers.FindAsync(id);
            if (s == null) return null;

            s.Name = dto.Name;
            s.Phone = dto.Phone;
            s.Email = dto.Email;
            s.Address = dto.Address;
            s.TaxRegistrationNumber = dto.TaxRegistrationNumber;
            s.CommercialRegister = dto.CommercialRegister;

            await _context.SaveChangesAsync();
            return Map(s);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var s = await _context.Suppliers.FindAsync(id);
            if (s == null) return false;
            _context.Suppliers.Remove(s);
            await _context.SaveChangesAsync();
            return true;
        }

        private static SupplierDto Map(Supplier s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            TaxRegistrationNumber = s.TaxRegistrationNumber,
            CommercialRegister = s.CommercialRegister,
            Balance = s.Balance,
            IsActive = s.IsActive
        };
    }
}
