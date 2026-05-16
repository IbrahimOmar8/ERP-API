using Application.DTOs.Delivery;
using Application.Inerfaces.Delivery;
using Domain.Models.Delivery;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Delivery
{
    public class DeliveryZoneService : IDeliveryZoneService
    {
        private readonly ApplicationDbContext _context;
        public DeliveryZoneService(ApplicationDbContext context) => _context = context;

        public async Task<List<DeliveryZoneDto>> GetAllAsync(CancellationToken ct = default) =>
            await _context.DeliveryZones.OrderBy(z => z.Name)
                .Select(z => new DeliveryZoneDto
                {
                    Id = z.Id, Name = z.Name, Fee = z.Fee,
                    EstimatedMinutes = z.EstimatedMinutes, IsActive = z.IsActive,
                }).ToListAsync(ct);

        public async Task<DeliveryZoneDto> CreateAsync(CreateZoneDto dto, CancellationToken ct = default)
        {
            var z = new DeliveryZone
            {
                Name = dto.Name, Fee = dto.Fee,
                EstimatedMinutes = dto.EstimatedMinutes, IsActive = dto.IsActive,
            };
            _context.DeliveryZones.Add(z);
            await _context.SaveChangesAsync(ct);
            return new DeliveryZoneDto
            {
                Id = z.Id, Name = z.Name, Fee = z.Fee,
                EstimatedMinutes = z.EstimatedMinutes, IsActive = z.IsActive,
            };
        }

        public async Task<DeliveryZoneDto?> UpdateAsync(Guid id, CreateZoneDto dto, CancellationToken ct = default)
        {
            var z = await _context.DeliveryZones.FindAsync(new object?[] { id }, ct);
            if (z == null) return null;
            z.Name = dto.Name; z.Fee = dto.Fee;
            z.EstimatedMinutes = dto.EstimatedMinutes; z.IsActive = dto.IsActive;
            await _context.SaveChangesAsync(ct);
            return new DeliveryZoneDto
            {
                Id = z.Id, Name = z.Name, Fee = z.Fee,
                EstimatedMinutes = z.EstimatedMinutes, IsActive = z.IsActive,
            };
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var z = await _context.DeliveryZones.FindAsync(new object?[] { id }, ct);
            if (z == null) return false;
            if (await _context.DeliveryOrders.AnyAsync(o => o.ZoneId == id, ct))
                throw new InvalidOperationException("لا يمكن حذف منطقة لديها طلبات سابقة");
            _context.DeliveryZones.Remove(z);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
