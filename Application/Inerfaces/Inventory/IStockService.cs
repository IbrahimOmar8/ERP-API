using Application.DTOs.Inventory;
using Domain.Enums;

namespace Application.Inerfaces.Inventory
{
    public interface IStockService
    {
        Task<List<StockItemDto>> GetStockByWarehouseAsync(Guid warehouseId);
        Task<List<StockItemDto>> GetStockByProductAsync(Guid productId);
        Task<StockItemDto?> GetStockAsync(Guid productId, Guid warehouseId);
        Task<List<StockMovementDto>> GetMovementsAsync(Guid productId, Guid? warehouseId = null);
        Task<bool> AdjustStockAsync(StockAdjustmentDto dto, Guid? userId);

        // Apply a movement (increase / decrease) and update StockItem balance + average cost
        Task ApplyMovementAsync(Guid productId, Guid warehouseId, MovementType type,
            decimal quantity, decimal unitCost, Guid? referenceId, string? referenceType,
            string? documentNumber, Guid? userId);
    }
}
