using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Domain.Enums;
using Domain.Models.Inventory;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Inventory
{
    public class StockTransferService : IStockTransferService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockService _stock;

        public StockTransferService(ApplicationDbContext context, IStockService stock)
        {
            _context = context;
            _stock = stock;
        }

        public async Task<List<StockTransferDto>> GetAllAsync(Guid? warehouseId = null)
        {
            var q = _context.StockTransfers
                .Include(t => t.FromWarehouse)
                .Include(t => t.ToWarehouse)
                .Include(t => t.Items!).ThenInclude(i => i.Product)
                .OrderByDescending(t => t.TransferDate)
                .AsQueryable();

            if (warehouseId.HasValue)
                q = q.Where(t => t.FromWarehouseId == warehouseId.Value || t.ToWarehouseId == warehouseId.Value);

            var list = await q.ToListAsync();
            return list.Select(Map).ToList();
        }

        public async Task<StockTransferDto?> GetByIdAsync(Guid id)
        {
            var t = await _context.StockTransfers
                .Include(t => t.FromWarehouse)
                .Include(t => t.ToWarehouse)
                .Include(t => t.Items!).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(t => t.Id == id);
            return t == null ? null : Map(t);
        }

        public async Task<StockTransferDto> CreateAsync(CreateStockTransferDto dto, Guid? userId)
        {
            if (dto.FromWarehouseId == dto.ToWarehouseId)
                throw new InvalidOperationException("لا يمكن التحويل لنفس المخزن");
            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("التحويل بدون أصناف");

            var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
            var sourceStocks = await _context.StockItems
                .Where(s => s.WarehouseId == dto.FromWarehouseId && productIds.Contains(s.ProductId))
                .ToDictionaryAsync(s => s.ProductId);

            foreach (var line in dto.Items)
            {
                if (!sourceStocks.TryGetValue(line.ProductId, out var stock) || stock.Quantity < line.Quantity)
                    throw new InvalidOperationException($"رصيد غير كافٍ للمنتج {line.ProductId}");
            }

            var transfer = new StockTransfer
            {
                TransferNumber = await NextNumberAsync(),
                FromWarehouseId = dto.FromWarehouseId,
                ToWarehouseId = dto.ToWarehouseId,
                Notes = dto.Notes,
                CreatedByUserId = userId,
                TransferDate = DateTime.UtcNow,
                IsCompleted = false,
                Items = dto.Items.Select(i => new StockTransferItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitCost = sourceStocks[i.ProductId].AverageCost
                }).ToList()
            };

            _context.StockTransfers.Add(transfer);
            await _context.SaveChangesAsync();

            return (await GetByIdAsync(transfer.Id))!;
        }

        public async Task<bool> CompleteAsync(Guid id, Guid? userId)
        {
            var t = await _context.StockTransfers
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (t == null) return false;
            if (t.IsCompleted) throw new InvalidOperationException("التحويل مكتمل مسبقاً");

            foreach (var line in t.Items ?? Enumerable.Empty<StockTransferItem>())
            {
                await _stock.ApplyMovementAsync(line.ProductId, t.FromWarehouseId,
                    MovementType.TransferOut, line.Quantity, line.UnitCost, t.Id, "Transfer",
                    t.TransferNumber, userId);
                await _stock.ApplyMovementAsync(line.ProductId, t.ToWarehouseId,
                    MovementType.TransferIn, line.Quantity, line.UnitCost, t.Id, "Transfer",
                    t.TransferNumber, userId);
            }

            t.IsCompleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelAsync(Guid id)
        {
            var t = await _context.StockTransfers.FindAsync(id);
            if (t == null) return false;
            if (t.IsCompleted)
                throw new InvalidOperationException("لا يمكن إلغاء تحويل مكتمل");
            _context.StockTransfers.Remove(t);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<string> NextNumberAsync()
        {
            var prefix = $"TR-{DateTime.UtcNow:yyyyMM}-";
            var last = await _context.StockTransfers
                .Where(t => t.TransferNumber.StartsWith(prefix))
                .OrderByDescending(t => t.TransferNumber)
                .Select(t => t.TransferNumber)
                .FirstOrDefaultAsync();
            var next = 1;
            if (last != null && int.TryParse(last[prefix.Length..], out var n)) next = n + 1;
            return $"{prefix}{next:D4}";
        }

        private static StockTransferDto Map(StockTransfer t) => new()
        {
            Id = t.Id,
            TransferNumber = t.TransferNumber,
            FromWarehouseId = t.FromWarehouseId,
            FromWarehouseName = t.FromWarehouse?.NameAr ?? string.Empty,
            ToWarehouseId = t.ToWarehouseId,
            ToWarehouseName = t.ToWarehouse?.NameAr ?? string.Empty,
            TransferDate = t.TransferDate,
            IsCompleted = t.IsCompleted,
            Notes = t.Notes,
            Items = (t.Items ?? Enumerable.Empty<StockTransferItem>()).Select(i => new StockTransferItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.NameAr ?? string.Empty,
                Sku = i.Product?.Sku ?? string.Empty,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList()
        };
    }
}
