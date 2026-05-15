using System.Text.Json;
using Application.DTOs.POS;
using Application.Inerfaces.POS;
using Domain.Models.POS;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.POS
{
    public class HeldOrderService : IHeldOrderService
    {
        private readonly ApplicationDbContext _context;
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public HeldOrderService(ApplicationDbContext context) => _context = context;

        public async Task<List<HeldOrderSummaryDto>> GetAllAsync(Guid? cashierUserId, CancellationToken ct = default)
        {
            var q = _context.HeldOrders.AsQueryable();
            if (cashierUserId.HasValue)
                q = q.Where(o => o.CashierUserId == cashierUserId.Value);

            // Join with customer name without loading the full entity
            var rows = await q
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id, o.Label, o.CashierUserId, o.CustomerId,
                    o.ItemCount, o.TotalEstimate, o.Notes, o.CreatedAt,
                    CustomerName = _context.Customers
                        .Where(c => c.Id == o.CustomerId)
                        .Select(c => c.Name)
                        .FirstOrDefault(),
                })
                .ToListAsync(ct);

            return rows.Select(r => new HeldOrderSummaryDto
            {
                Id = r.Id,
                Label = r.Label,
                CashierUserId = r.CashierUserId,
                CustomerId = r.CustomerId,
                CustomerName = r.CustomerName,
                ItemCount = r.ItemCount,
                TotalEstimate = r.TotalEstimate,
                Notes = r.Notes,
                CreatedAt = r.CreatedAt,
            }).ToList();
        }

        public async Task<HeldOrderDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var order = await _context.HeldOrders.FindAsync(new object?[] { id }, ct);
            if (order == null) return null;

            var items = SafeDeserialize(order.ItemsJson);
            // Backfill names — JSON is stale if a product was renamed since
            var productIds = items.Select(i => i.ProductId).ToList();
            var names = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.NameAr })
                .ToDictionaryAsync(p => p.Id, p => p.NameAr, ct);

            foreach (var item in items)
                if (names.TryGetValue(item.ProductId, out var name))
                    item.ProductName = name;

            string? customerName = null;
            if (order.CustomerId.HasValue)
                customerName = await _context.Customers
                    .Where(c => c.Id == order.CustomerId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync(ct);

            return new HeldOrderDetailDto
            {
                Id = order.Id,
                Label = order.Label,
                CashierUserId = order.CashierUserId,
                CustomerId = order.CustomerId,
                CustomerName = customerName,
                ItemCount = order.ItemCount,
                TotalEstimate = order.TotalEstimate,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                Items = items,
            };
        }

        public async Task<HeldOrderSummaryDto> CreateAsync(CreateHeldOrderDto dto, Guid cashierUserId, CancellationToken ct = default)
        {
            // Snapshot product names so the held order displays even if a
            // product is later deleted
            var productIds = dto.Items.Select(i => i.ProductId).ToList();
            var names = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.NameAr })
                .ToDictionaryAsync(p => p.Id, p => p.NameAr, ct);

            foreach (var item in dto.Items)
                if (item.ProductName == null && names.TryGetValue(item.ProductId, out var name))
                    item.ProductName = name;

            var totalEstimate = dto.Items.Sum(i =>
                (i.Quantity * i.UnitPrice) - i.DiscountAmount);

            var entity = new HeldOrder
            {
                Label = dto.Label,
                CustomerId = dto.CustomerId,
                CashSessionId = dto.CashSessionId,
                CashierUserId = cashierUserId,
                Notes = dto.Notes,
                ItemCount = dto.Items.Count,
                TotalEstimate = totalEstimate,
                ItemsJson = JsonSerializer.Serialize(dto.Items, JsonOpts),
            };
            _context.HeldOrders.Add(entity);
            await _context.SaveChangesAsync(ct);

            return new HeldOrderSummaryDto
            {
                Id = entity.Id,
                Label = entity.Label,
                CashierUserId = entity.CashierUserId,
                CustomerId = entity.CustomerId,
                ItemCount = entity.ItemCount,
                TotalEstimate = entity.TotalEstimate,
                Notes = entity.Notes,
                CreatedAt = entity.CreatedAt,
            };
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var order = await _context.HeldOrders.FindAsync(new object?[] { id }, ct);
            if (order == null) return false;
            _context.HeldOrders.Remove(order);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private static List<HeldOrderItem> SafeDeserialize(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<HeldOrderItem>>(json, JsonOpts) ?? new();
            }
            catch
            {
                return new List<HeldOrderItem>();
            }
        }
    }
}
