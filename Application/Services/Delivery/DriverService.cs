using Application.DTOs.Delivery;
using Application.Inerfaces.Delivery;
using Domain.Enums;
using Domain.Models.Delivery;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Delivery
{
    public class DriverService : IDriverService
    {
        private readonly ApplicationDbContext _context;
        public DriverService(ApplicationDbContext context) => _context = context;

        public async Task<List<DriverDto>> GetAllAsync(bool? activeOnly, CancellationToken ct = default)
        {
            var q = _context.Drivers.AsQueryable();
            if (activeOnly == true) q = q.Where(d => d.IsActive);

            var drivers = await q.OrderBy(d => d.Name).ToListAsync(ct);
            var activeOrders = await _context.DeliveryOrders
                .Where(o => o.DriverId != null
                            && o.Status != DeliveryStatus.Delivered
                            && o.Status != DeliveryStatus.Cancelled
                            && o.Status != DeliveryStatus.Returned)
                .GroupBy(o => o.DriverId!.Value)
                .Select(g => new { DriverId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.DriverId, x => x.Count, ct);

            return drivers.Select(d => Map(d, activeOrders.GetValueOrDefault(d.Id))).ToList();
        }

        public async Task<DriverDto> CreateAsync(CreateDriverDto dto, CancellationToken ct = default)
        {
            var d = new Driver
            {
                Name = dto.Name, Phone = dto.Phone, NationalId = dto.NationalId,
                VehicleType = dto.VehicleType, VehicleNumber = dto.VehicleNumber,
                CommissionPerDelivery = dto.CommissionPerDelivery,
                IsActive = dto.IsActive, Notes = dto.Notes,
            };
            _context.Drivers.Add(d);
            await _context.SaveChangesAsync(ct);
            return Map(d, 0);
        }

        public async Task<DriverDto?> UpdateAsync(Guid id, CreateDriverDto dto, CancellationToken ct = default)
        {
            var d = await _context.Drivers.FindAsync(new object?[] { id }, ct);
            if (d == null) return null;
            d.Name = dto.Name; d.Phone = dto.Phone; d.NationalId = dto.NationalId;
            d.VehicleType = dto.VehicleType; d.VehicleNumber = dto.VehicleNumber;
            d.CommissionPerDelivery = dto.CommissionPerDelivery;
            d.IsActive = dto.IsActive; d.Notes = dto.Notes;
            await _context.SaveChangesAsync(ct);
            return Map(d, await _context.DeliveryOrders.CountAsync(o => o.DriverId == id
                && o.Status != DeliveryStatus.Delivered
                && o.Status != DeliveryStatus.Cancelled
                && o.Status != DeliveryStatus.Returned, ct));
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var d = await _context.Drivers.FindAsync(new object?[] { id }, ct);
            if (d == null) return false;
            if (await _context.DeliveryOrders.AnyAsync(o => o.DriverId == id, ct))
                throw new InvalidOperationException("لا يمكن حذف مندوب لديه طلبات — قم بتعطيله بدلاً من الحذف");
            _context.Drivers.Remove(d);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private static DriverDto Map(Driver d, int activeOrders) => new()
        {
            Id = d.Id, Name = d.Name, Phone = d.Phone, NationalId = d.NationalId,
            VehicleType = d.VehicleType, VehicleNumber = d.VehicleNumber,
            CommissionPerDelivery = d.CommissionPerDelivery,
            IsActive = d.IsActive, Notes = d.Notes,
            ActiveOrders = activeOrders, CashHeld = 0,
        };
    }
}
