using Application.DTOs.Production;
using Application.Inerfaces.Inventory;
using Application.Inerfaces.Production;
using Domain.Enums;
using Domain.Models.Production;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Production
{
    public class ProductionOrderService : IProductionOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockService _stock;

        public ProductionOrderService(ApplicationDbContext context, IStockService stock)
        {
            _context = context;
            _stock = stock;
        }

        public async Task<List<ProductionOrderDto>> GetAllAsync(CancellationToken ct = default)
        {
            var orders = await _context.ProductionOrders
                .Include(o => o.Items)
                .OrderByDescending(o => o.PlannedDate)
                .ToListAsync(ct);

            var result = new List<ProductionOrderDto>(orders.Count);
            foreach (var o in orders) result.Add(await MapAsync(o, ct));
            return result;
        }

        public async Task<ProductionOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var o = await _context.ProductionOrders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, ct);
            return o == null ? null : await MapAsync(o, ct);
        }

        public async Task<ProductionOrderDto> CreateAsync(CreateProductionOrderDto dto, Guid? userId, CancellationToken ct = default)
        {
            var bom = await _context.BillsOfMaterials.Include(b => b.Components).FirstOrDefaultAsync(b => b.Id == dto.BillOfMaterialsId, ct)
                ?? throw new InvalidOperationException("الوصفة غير موجودة");
            if (!bom.IsActive)
                throw new InvalidOperationException("الوصفة غير نشطة");

            var nextNumber = await NextOrderNumberAsync(ct);

            var order = new ProductionOrder
            {
                OrderNumber = nextNumber,
                BillOfMaterialsId = bom.Id,
                ProductId = bom.ProductId,
                WarehouseId = dto.WarehouseId,
                Quantity = dto.Quantity,
                PlannedDate = dto.PlannedDate ?? DateTime.UtcNow,
                Notes = dto.Notes,
                CreatedByUserId = userId,
                Status = ProductionOrderStatus.Draft,
            };

            // Snapshot the planned components (using BOM output ratio + waste)
            var batches = bom.OutputQuantity > 0 ? dto.Quantity / bom.OutputQuantity : 0;
            foreach (var c in bom.Components)
            {
                var qty = c.Quantity * batches * (1 + c.WastePercent / 100m);
                order.Items.Add(new ProductionOrderItem
                {
                    ProductId = c.ProductId,
                    Quantity = Math.Round(qty, 4),
                });
            }

            _context.ProductionOrders.Add(order);
            await _context.SaveChangesAsync(ct);
            return (await GetByIdAsync(order.Id, ct))!;
        }

        public async Task<ProductionOrderDto?> CompleteAsync(Guid id, Guid? userId, CancellationToken ct = default)
        {
            var order = await _context.ProductionOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
            if (order == null) return null;
            if (order.Status == ProductionOrderStatus.Completed)
                throw new InvalidOperationException("أمر الإنتاج مكتمل بالفعل");
            if (order.Status == ProductionOrderStatus.Cancelled)
                throw new InvalidOperationException("لا يمكن إكمال أمر ملغى");

            var bom = await _context.BillsOfMaterials.FindAsync(new object?[] { order.BillOfMaterialsId }, ct)
                ?? throw new InvalidOperationException("الوصفة غير موجودة");

            // Verify stock availability for every component
            var componentIds = order.Items.Select(i => i.ProductId).ToList();
            var stocks = await _context.StockItems
                .Where(s => componentIds.Contains(s.ProductId) && s.WarehouseId == order.WarehouseId)
                .ToDictionaryAsync(s => s.ProductId, s => s, ct);

            decimal totalCost = 0;
            foreach (var item in order.Items)
            {
                var stock = stocks.TryGetValue(item.ProductId, out var s) ? s : null;
                var available = stock?.Quantity ?? 0;
                if (available < item.Quantity)
                {
                    var product = await _context.Products.FindAsync(new object?[] { item.ProductId }, ct);
                    throw new InvalidOperationException(
                        $"رصيد غير كاف من \"{product?.NameAr}\" — المطلوب {item.Quantity:0.##}, المتاح {available:0.##}");
                }
                var cost = stock?.AverageCost ?? 0;
                item.UnitCost = cost;
                item.TotalCost = Math.Round(cost * item.Quantity, 4);
                totalCost += item.TotalCost;
            }

            totalCost += bom.AdditionalCostPerUnit * order.Quantity;
            order.TotalCost = Math.Round(totalCost, 4);
            order.UnitCost = order.Quantity > 0 ? Math.Round(totalCost / order.Quantity, 4) : 0;

            // Consume components
            foreach (var item in order.Items)
            {
                await _stock.ApplyMovementAsync(item.ProductId, order.WarehouseId,
                    MovementType.ProductionConsume, item.Quantity, item.UnitCost,
                    order.Id, "ProductionOrder", order.OrderNumber, userId);
            }

            // Produce finished good
            await _stock.ApplyMovementAsync(order.ProductId, order.WarehouseId,
                MovementType.ProductionOutput, order.Quantity, order.UnitCost,
                order.Id, "ProductionOrder", order.OrderNumber, userId);

            order.Status = ProductionOrderStatus.Completed;
            order.CompletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(order.Id, ct);
        }

        public async Task<ProductionOrderDto?> CancelAsync(Guid id, CancellationToken ct = default)
        {
            var order = await _context.ProductionOrders.FindAsync(new object?[] { id }, ct);
            if (order == null) return null;
            if (order.Status == ProductionOrderStatus.Completed)
                throw new InvalidOperationException("لا يمكن إلغاء أمر مكتمل");
            order.Status = ProductionOrderStatus.Cancelled;
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var order = await _context.ProductionOrders.FindAsync(new object?[] { id }, ct);
            if (order == null) return false;
            if (order.Status == ProductionOrderStatus.Completed)
                throw new InvalidOperationException("لا يمكن حذف أمر مكتمل — استخدم الإلغاء");
            _context.ProductionOrders.Remove(order);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private async Task<string> NextOrderNumberAsync(CancellationToken ct)
        {
            // Counts existing orders + 1, formatted as PRD-0001
            var count = await _context.ProductionOrders.CountAsync(ct);
            return $"PRD-{(count + 1):D4}";
        }

        private async Task<ProductionOrderDto> MapAsync(ProductionOrder o, CancellationToken ct)
        {
            var bomName = await _context.BillsOfMaterials.Where(b => b.Id == o.BillOfMaterialsId).Select(b => b.Name).FirstOrDefaultAsync(ct);
            var productName = await _context.Products.Where(p => p.Id == o.ProductId).Select(p => p.NameAr).FirstOrDefaultAsync(ct);
            var warehouseName = await _context.Warehouses.Where(w => w.Id == o.WarehouseId).Select(w => w.NameAr).FirstOrDefaultAsync(ct);

            var itemIds = o.Items.Select(i => i.ProductId).ToList();
            var itemNames = await _context.Products.Where(p => itemIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.NameAr, ct);

            return new ProductionOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                BillOfMaterialsId = o.BillOfMaterialsId, BomName = bomName,
                ProductId = o.ProductId, ProductName = productName,
                WarehouseId = o.WarehouseId, WarehouseName = warehouseName,
                Quantity = o.Quantity,
                TotalCost = o.TotalCost, UnitCost = o.UnitCost,
                Status = o.Status,
                PlannedDate = o.PlannedDate, CompletedDate = o.CompletedDate,
                Notes = o.Notes,
                Items = o.Items.Select(i => new ProductionOrderItemDto
                {
                    Id = i.Id, ProductId = i.ProductId,
                    ProductName = itemNames.GetValueOrDefault(i.ProductId),
                    Quantity = i.Quantity,
                    UnitCost = i.UnitCost, TotalCost = i.TotalCost,
                }).ToList(),
            };
        }
    }
}
