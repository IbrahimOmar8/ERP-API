using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Domain.Enums;
using Domain.Models.Inventory;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Inventory
{
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _context;

        public StockService(ApplicationDbContext context) => _context = context;

        public async Task<List<StockItemDto>> GetStockByWarehouseAsync(Guid warehouseId)
        {
            return await _context.StockItems
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Warehouse)
                .Where(s => s.WarehouseId == warehouseId)
                .Select(s => MapItem(s))
                .ToListAsync();
        }

        public async Task<List<StockItemDto>> GetStockByProductAsync(Guid productId)
        {
            return await _context.StockItems
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Warehouse)
                .Where(s => s.ProductId == productId)
                .Select(s => MapItem(s))
                .ToListAsync();
        }

        public async Task<StockItemDto?> GetStockAsync(Guid productId, Guid warehouseId)
        {
            var item = await _context.StockItems
                .AsNoTracking()
                .Include(s => s.Product)
                .Include(s => s.Warehouse)
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId);

            return item == null ? null : MapItem(item);
        }

        public async Task<List<StockMovementDto>> GetMovementsAsync(Guid productId, Guid? warehouseId = null)
        {
            var query = _context.StockMovements
                .AsNoTracking()
                .Include(m => m.Product)
                .Include(m => m.Warehouse)
                .Where(m => m.ProductId == productId);

            if (warehouseId.HasValue)
                query = query.Where(m => m.WarehouseId == warehouseId.Value);

            return await query
                .OrderByDescending(m => m.MovementDate)
                .Select(m => new StockMovementDto
                {
                    Id = m.Id,
                    ProductId = m.ProductId,
                    ProductName = m.Product != null ? m.Product.NameAr : string.Empty,
                    WarehouseId = m.WarehouseId,
                    WarehouseName = m.Warehouse != null ? m.Warehouse.NameAr : string.Empty,
                    Type = m.Type,
                    Quantity = m.Quantity,
                    UnitCost = m.UnitCost,
                    BalanceAfter = m.BalanceAfter,
                    DocumentNumber = m.DocumentNumber,
                    Notes = m.Notes,
                    MovementDate = m.MovementDate
                })
                .ToListAsync();
        }

        public async Task<bool> AdjustStockAsync(StockAdjustmentDto dto, Guid? userId)
        {
            var stockItem = await _context.StockItems
                .FirstOrDefaultAsync(s => s.ProductId == dto.ProductId && s.WarehouseId == dto.WarehouseId);

            decimal currentQty = stockItem?.Quantity ?? 0;
            decimal diff = dto.NewQuantity - currentQty;

            if (diff == 0) return true;

            var product = await _context.Products.FindAsync(dto.ProductId);
            decimal unitCost = product?.PurchasePrice ?? 0;

            var movementType = diff > 0 ? MovementType.AdjustmentIn : MovementType.AdjustmentOut;
            await ApplyMovementAsync(dto.ProductId, dto.WarehouseId, movementType,
                Math.Abs(diff), unitCost, null, "Adjustment", null, userId);

            return true;
        }

        public async Task ApplyMovementAsync(Guid productId, Guid warehouseId, MovementType type,
            decimal quantity, decimal unitCost, Guid? referenceId, string? referenceType,
            string? documentNumber, Guid? userId)
        {
            var stockItem = await _context.StockItems
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId);

            if (stockItem == null)
            {
                stockItem = new StockItem
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    Quantity = 0,
                    AverageCost = unitCost
                };
                _context.StockItems.Add(stockItem);
            }

            bool isIncrease = type == MovementType.PurchaseIn
                || type == MovementType.TransferIn
                || type == MovementType.AdjustmentIn
                || type == MovementType.ReturnIn
                || type == MovementType.OpeningBalance;

            if (isIncrease)
            {
                // Weighted average cost
                var totalValue = stockItem.Quantity * stockItem.AverageCost + quantity * unitCost;
                var totalQty = stockItem.Quantity + quantity;
                stockItem.AverageCost = totalQty > 0 ? totalValue / totalQty : unitCost;
                stockItem.Quantity += quantity;
            }
            else
            {
                stockItem.Quantity -= quantity;
            }

            stockItem.LastUpdated = DateTime.UtcNow;

            var movement = new StockMovement
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                Type = type,
                Quantity = quantity,
                UnitCost = unitCost,
                BalanceAfter = stockItem.Quantity,
                ReferenceId = referenceId,
                ReferenceType = referenceType,
                DocumentNumber = documentNumber,
                CreatedByUserId = userId
            };
            _context.StockMovements.Add(movement);
            await _context.SaveChangesAsync();
        }

        private static StockItemDto MapItem(StockItem s) => new()
        {
            Id = s.Id,
            ProductId = s.ProductId,
            ProductName = s.Product?.NameAr ?? string.Empty,
            Sku = s.Product?.Sku ?? string.Empty,
            WarehouseId = s.WarehouseId,
            WarehouseName = s.Warehouse?.NameAr ?? string.Empty,
            Quantity = s.Quantity,
            ReservedQuantity = s.ReservedQuantity,
            AverageCost = s.AverageCost,
            LastUpdated = s.LastUpdated
        };
    }
}
