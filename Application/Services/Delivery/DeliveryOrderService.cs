using Application.DTOs.Delivery;
using Application.Inerfaces.Delivery;
using Domain.Enums;
using Domain.Models.Delivery;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Delivery
{
    public class DeliveryOrderService : IDeliveryOrderService
    {
        private readonly ApplicationDbContext _context;
        public DeliveryOrderService(ApplicationDbContext context) => _context = context;

        public async Task<List<DeliveryOrderDto>> GetAsync(DeliveryFilterDto filter, CancellationToken ct = default)
        {
            var q = _context.DeliveryOrders.AsQueryable();
            if (filter.Status.HasValue) q = q.Where(o => o.Status == filter.Status.Value);
            if (filter.DriverId.HasValue) q = q.Where(o => o.DriverId == filter.DriverId.Value);
            if (filter.ZoneId.HasValue) q = q.Where(o => o.ZoneId == filter.ZoneId.Value);
            if (filter.From.HasValue) q = q.Where(o => o.CreatedAt >= filter.From.Value);
            if (filter.To.HasValue) q = q.Where(o => o.CreatedAt <= filter.To.Value);

            var rows = await q.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
            return await MapManyAsync(rows, ct);
        }

        public async Task<DeliveryOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var o = await _context.DeliveryOrders.FindAsync(new object?[] { id }, ct);
            if (o == null) return null;
            return (await MapManyAsync(new[] { o }, ct)).FirstOrDefault();
        }

        public async Task<DeliveryOrderDto> CreateAsync(CreateDeliveryOrderDto dto, Guid? userId, CancellationToken ct = default)
        {
            var orderNumber = await NextOrderNumberAsync(ct);

            var order = new DeliveryOrder
            {
                OrderNumber = orderNumber,
                SaleId = dto.SaleId,
                CustomerId = dto.CustomerId,
                CustomerName = dto.CustomerName,
                CustomerPhone = dto.CustomerPhone,
                Address = dto.Address,
                ZoneId = dto.ZoneId,
                DeliveryFee = dto.DeliveryFee,
                CashToCollect = dto.CashToCollect,
                DriverId = dto.DriverId,
                Status = dto.DriverId.HasValue ? DeliveryStatus.Assigned : DeliveryStatus.Pending,
                AssignedAt = dto.DriverId.HasValue ? DateTime.UtcNow : null,
                Notes = dto.Notes,
                CreatedByUserId = userId,
            };
            _context.DeliveryOrders.Add(order);
            await _context.SaveChangesAsync(ct);
            return (await GetByIdAsync(order.Id, ct))!;
        }

        public async Task<DeliveryOrderDto?> AssignAsync(Guid id, Guid driverId, CancellationToken ct = default)
        {
            var o = await _context.DeliveryOrders.FindAsync(new object?[] { id }, ct);
            if (o == null) return null;
            if (o.Status == DeliveryStatus.Delivered || o.Status == DeliveryStatus.Cancelled)
                throw new InvalidOperationException("لا يمكن إسناد طلب منتهي");

            o.DriverId = driverId;
            if (o.Status == DeliveryStatus.Pending)
            {
                o.Status = DeliveryStatus.Assigned;
                o.AssignedAt = DateTime.UtcNow;
            }
            o.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<DeliveryOrderDto?> PickUpAsync(Guid id, CancellationToken ct = default)
        {
            var o = await _context.DeliveryOrders.FindAsync(new object?[] { id }, ct);
            if (o == null) return null;
            if (o.DriverId == null)
                throw new InvalidOperationException("لا يمكن استلام الطلب قبل إسناد مندوب");
            if (o.Status != DeliveryStatus.Assigned)
                throw new InvalidOperationException("الطلب ليس في حالة إسناد");

            o.Status = DeliveryStatus.PickedUp;
            o.PickedUpAt = DateTime.UtcNow;
            o.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<DeliveryOrderDto?> DeliverAsync(Guid id, DeliverDto dto, Guid? userId, CancellationToken ct = default)
        {
            var o = await _context.DeliveryOrders.FindAsync(new object?[] { id }, ct);
            if (o == null) return null;
            if (o.Status != DeliveryStatus.PickedUp && o.Status != DeliveryStatus.Assigned)
                throw new InvalidOperationException("لا يمكن تسليم الطلب من حالته الحالية");

            o.Status = DeliveryStatus.Delivered;
            o.DeliveredAt = DateTime.UtcNow;
            o.CashCollected = dto.CashCollected ?? o.CashToCollect;
            o.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<DeliveryOrderDto?> CancelAsync(Guid id, CancellationToken ct = default)
        {
            var o = await _context.DeliveryOrders.FindAsync(new object?[] { id }, ct);
            if (o == null) return null;
            if (o.Status == DeliveryStatus.Delivered)
                throw new InvalidOperationException("لا يمكن إلغاء طلب تم تسليمه");
            o.Status = DeliveryStatus.Cancelled;
            o.CancelledAt = DateTime.UtcNow;
            o.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<DeliveryOrderDto?> ReturnAsync(Guid id, CancellationToken ct = default)
        {
            var o = await _context.DeliveryOrders.FindAsync(new object?[] { id }, ct);
            if (o == null) return null;
            if (o.Status != DeliveryStatus.PickedUp)
                throw new InvalidOperationException("يمكن إرجاع الطلبات المُستلمة فقط من المندوب");
            o.Status = DeliveryStatus.Returned;
            o.CashCollected = 0;
            o.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var o = await _context.DeliveryOrders.FindAsync(new object?[] { id }, ct);
            if (o == null) return false;
            if (o.Status == DeliveryStatus.Delivered)
                throw new InvalidOperationException("لا يمكن حذف طلب تم تسليمه");
            _context.DeliveryOrders.Remove(o);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<DriverReconciliationRow>> GetReconciliationAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            var grouped = await _context.DeliveryOrders
                .Where(o => o.Status == DeliveryStatus.Delivered
                            && o.DriverId != null
                            && o.DeliveredAt >= from && o.DeliveredAt < to)
                .GroupBy(o => o.DriverId!.Value)
                .Select(g => new
                {
                    DriverId = g.Key,
                    Count = g.Count(),
                    Cash = g.Sum(x => x.CashCollected),
                })
                .ToListAsync(ct);

            if (grouped.Count == 0) return new List<DriverReconciliationRow>();

            var ids = grouped.Select(x => x.DriverId).ToList();
            var drivers = await _context.Drivers.Where(d => ids.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, d => new { d.Name, d.CommissionPerDelivery }, ct);

            return grouped.Select(g =>
            {
                drivers.TryGetValue(g.DriverId, out var info);
                return new DriverReconciliationRow
                {
                    DriverId = g.DriverId,
                    DriverName = info?.Name ?? "—",
                    DeliveredCount = g.Count,
                    CashCollected = g.Cash,
                    Commission = (info?.CommissionPerDelivery ?? 0) * g.Count,
                };
            }).OrderByDescending(r => r.DeliveredCount).ToList();
        }

        private async Task<string> NextOrderNumberAsync(CancellationToken ct)
        {
            var count = await _context.DeliveryOrders.CountAsync(ct);
            return $"DLV-{(count + 1):D4}";
        }

        private async Task<List<DeliveryOrderDto>> MapManyAsync(IEnumerable<DeliveryOrder> rows, CancellationToken ct)
        {
            var driverIds = rows.Where(r => r.DriverId.HasValue).Select(r => r.DriverId!.Value).Distinct().ToList();
            var zoneIds = rows.Where(r => r.ZoneId.HasValue).Select(r => r.ZoneId!.Value).Distinct().ToList();
            var custIds = rows.Where(r => r.CustomerId.HasValue).Select(r => r.CustomerId!.Value).Distinct().ToList();
            var saleIds = rows.Where(r => r.SaleId.HasValue).Select(r => r.SaleId!.Value).Distinct().ToList();

            var drivers = driverIds.Count == 0 ? new Dictionary<Guid, string>()
                : await _context.Drivers.Where(x => driverIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
            var zones = zoneIds.Count == 0 ? new Dictionary<Guid, string>()
                : await _context.DeliveryZones.Where(x => zoneIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
            var custs = custIds.Count == 0 ? new Dictionary<Guid, string>()
                : await _context.Customers.Where(x => custIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
            var sales = saleIds.Count == 0 ? new Dictionary<Guid, string>()
                : await _context.Sales.Where(x => saleIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.InvoiceNumber, ct);

            return rows.Select(o => new DeliveryOrderDto
            {
                Id = o.Id, OrderNumber = o.OrderNumber,
                SaleId = o.SaleId,
                SaleNumber = o.SaleId.HasValue ? sales.GetValueOrDefault(o.SaleId.Value) : null,
                CustomerId = o.CustomerId,
                CustomerName = o.CustomerName ?? (o.CustomerId.HasValue ? custs.GetValueOrDefault(o.CustomerId.Value) : null),
                CustomerPhone = o.CustomerPhone,
                Address = o.Address,
                ZoneId = o.ZoneId,
                ZoneName = o.ZoneId.HasValue ? zones.GetValueOrDefault(o.ZoneId.Value) : null,
                DeliveryFee = o.DeliveryFee,
                CashToCollect = o.CashToCollect,
                CashCollected = o.CashCollected,
                DriverId = o.DriverId,
                DriverName = o.DriverId.HasValue ? drivers.GetValueOrDefault(o.DriverId.Value) : null,
                Status = o.Status,
                AssignedAt = o.AssignedAt, PickedUpAt = o.PickedUpAt,
                DeliveredAt = o.DeliveredAt, CancelledAt = o.CancelledAt,
                Notes = o.Notes,
                CreatedAt = o.CreatedAt,
            }).ToList();
        }
    }
}
