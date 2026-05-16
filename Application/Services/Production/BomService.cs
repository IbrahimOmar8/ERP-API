using Application.DTOs.Production;
using Application.Inerfaces.Production;
using Domain.Models.Production;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Production
{
    public class BomService : IBomService
    {
        private readonly ApplicationDbContext _context;
        public BomService(ApplicationDbContext context) => _context = context;

        public async Task<List<BomDto>> GetAllAsync(Guid? productId = null, CancellationToken ct = default)
        {
            var q = _context.BillsOfMaterials.Include(b => b.Components).AsQueryable();
            if (productId.HasValue) q = q.Where(b => b.ProductId == productId.Value);
            var boms = await q.OrderBy(b => b.Name).ToListAsync(ct);
            var result = new List<BomDto>(boms.Count);
            foreach (var b in boms) result.Add(await MapAsync(b, ct));
            return result;
        }

        public async Task<BomDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var b = await _context.BillsOfMaterials.Include(x => x.Components).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (b == null) return null;
            return await MapAsync(b, ct);
        }

        public async Task<BomDto> CreateAsync(CreateBomDto dto, CancellationToken ct = default)
        {
            if (dto.Components.Count == 0)
                throw new InvalidOperationException("لا يمكن إنشاء وصفة بدون مكونات");
            if (dto.Components.Any(c => c.ProductId == dto.ProductId))
                throw new InvalidOperationException("لا يمكن أن يكون الناتج أحد المكونات");

            var b = new BillOfMaterials
            {
                ProductId = dto.ProductId,
                Name = dto.Name,
                OutputQuantity = dto.OutputQuantity <= 0 ? 1 : dto.OutputQuantity,
                AdditionalCostPerUnit = dto.AdditionalCostPerUnit,
                IsActive = dto.IsActive,
                Notes = dto.Notes,
                Components = dto.Components.Select(c => new BomComponent
                {
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    WastePercent = c.WastePercent,
                }).ToList(),
            };
            _context.BillsOfMaterials.Add(b);
            await _context.SaveChangesAsync(ct);
            return (await GetByIdAsync(b.Id, ct))!;
        }

        public async Task<BomDto?> UpdateAsync(Guid id, CreateBomDto dto, CancellationToken ct = default)
        {
            var b = await _context.BillsOfMaterials.Include(x => x.Components).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (b == null) return null;
            if (dto.Components.Count == 0)
                throw new InvalidOperationException("لا يمكن أن تكون الوصفة بلا مكونات");
            if (dto.Components.Any(c => c.ProductId == dto.ProductId))
                throw new InvalidOperationException("لا يمكن أن يكون الناتج أحد المكونات");

            b.ProductId = dto.ProductId;
            b.Name = dto.Name;
            b.OutputQuantity = dto.OutputQuantity <= 0 ? 1 : dto.OutputQuantity;
            b.AdditionalCostPerUnit = dto.AdditionalCostPerUnit;
            b.IsActive = dto.IsActive;
            b.Notes = dto.Notes;
            b.UpdatedAt = DateTime.UtcNow;

            _context.BomComponents.RemoveRange(b.Components);
            b.Components = dto.Components.Select(c => new BomComponent
            {
                BillOfMaterialsId = b.Id,
                ProductId = c.ProductId,
                Quantity = c.Quantity,
                WastePercent = c.WastePercent,
            }).ToList();

            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var b = await _context.BillsOfMaterials.FindAsync(new object?[] { id }, ct);
            if (b == null) return false;
            var used = await _context.ProductionOrders.AnyAsync(o => o.BillOfMaterialsId == id, ct);
            if (used) throw new InvalidOperationException("لا يمكن حذف وصفة مستخدمة في أوامر إنتاج");
            _context.BillsOfMaterials.Remove(b);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private async Task<BomDto> MapAsync(BillOfMaterials b, CancellationToken ct)
        {
            var productIds = b.Components.Select(c => c.ProductId).Append(b.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.NameAr, p.Sku, p.PurchasePrice })
                .ToDictionaryAsync(p => p.Id, p => p, ct);

            // Best-effort average cost from any warehouse — fall back to PurchasePrice
            var avgCosts = await _context.StockItems
                .Where(s => productIds.Contains(s.ProductId) && s.Quantity > 0)
                .GroupBy(s => s.ProductId)
                .Select(g => new { Id = g.Key, Cost = g.Average(x => x.AverageCost) })
                .ToDictionaryAsync(x => x.Id, x => x.Cost, ct);

            decimal componentsCost = 0;
            var compDtos = b.Components.Select(c =>
            {
                var p = products.TryGetValue(c.ProductId, out var info) ? info : null;
                var cost = avgCosts.TryGetValue(c.ProductId, out var ac) && ac > 0 ? ac : (p?.PurchasePrice ?? 0);
                var totalQty = c.Quantity * (1 + c.WastePercent / 100m);
                componentsCost += cost * totalQty;
                return new BomComponentDto
                {
                    Id = c.Id, ProductId = c.ProductId,
                    ProductName = p?.NameAr, ProductSku = p?.Sku,
                    Quantity = c.Quantity, WastePercent = c.WastePercent,
                    CurrentCost = cost,
                };
            }).ToList();

            var unitCost = b.OutputQuantity > 0
                ? (componentsCost / b.OutputQuantity) + b.AdditionalCostPerUnit
                : 0;

            return new BomDto
            {
                Id = b.Id,
                ProductId = b.ProductId,
                ProductName = products.TryGetValue(b.ProductId, out var fp) ? fp.NameAr : null,
                Name = b.Name,
                OutputQuantity = b.OutputQuantity,
                AdditionalCostPerUnit = b.AdditionalCostPerUnit,
                IsActive = b.IsActive,
                Notes = b.Notes,
                Components = compDtos,
                EstimatedUnitCost = Math.Round(unitCost, 4),
            };
        }
    }
}
