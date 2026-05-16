using Application.DTOs.POS;
using Application.Inerfaces.POS;
using Domain.Models.POS;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.POS
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;

        public CustomerService(ApplicationDbContext context) => _context = context;

        public async Task<List<CustomerDto>> GetAllAsync(string? search)
        {
            var query = _context.Customers.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(c => c.Name.Contains(s) ||
                    (c.Phone != null && c.Phone.Contains(s)) ||
                    (c.TaxRegistrationNumber != null && c.TaxRegistrationNumber.Contains(s)));
            }

            return await query.Select(c => Map(c)).ToListAsync();
        }

        public async Task<CustomerDto?> GetByIdAsync(Guid id)
        {
            var c = await _context.Customers.FindAsync(id);
            return c == null ? null : Map(c);
        }

        public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto)
        {
            var customer = new Customer
            {
                Name = dto.Name,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                TaxRegistrationNumber = dto.TaxRegistrationNumber,
                NationalId = dto.NationalId,
                IsCompany = dto.IsCompany,
                CreditLimit = dto.CreditLimit
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return Map(customer);
        }

        public async Task<CustomerDto?> UpdateAsync(Guid id, CreateCustomerDto dto)
        {
            var c = await _context.Customers.FindAsync(id);
            if (c == null) return null;

            c.Name = dto.Name;
            c.Phone = dto.Phone;
            c.Email = dto.Email;
            c.Address = dto.Address;
            c.TaxRegistrationNumber = dto.TaxRegistrationNumber;
            c.NationalId = dto.NationalId;
            c.IsCompany = dto.IsCompany;
            c.CreditLimit = dto.CreditLimit;

            await _context.SaveChangesAsync();
            return Map(c);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var c = await _context.Customers.FindAsync(id);
            if (c == null) return false;
            _context.Customers.Remove(c);
            await _context.SaveChangesAsync();
            return true;
        }

        private static CustomerDto Map(Customer c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Phone = c.Phone,
            Email = c.Email,
            Address = c.Address,
            TaxRegistrationNumber = c.TaxRegistrationNumber,
            NationalId = c.NationalId,
            IsCompany = c.IsCompany,
            Balance = c.Balance,
            CreditLimit = c.CreditLimit,
            IsActive = c.IsActive
        };
    }
}
