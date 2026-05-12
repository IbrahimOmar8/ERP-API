using Domain.Enums;

namespace Application.DTOs.Inventory
{
    public class StockItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal AvailableQuantity => Quantity - ReservedQuantity;
        public decimal AverageCost { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class StockMovementDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public MovementType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? DocumentNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime MovementDate { get; set; }
    }

    public class StockAdjustmentDto
    {
        public Guid ProductId { get; set; }
        public Guid WarehouseId { get; set; }
        public decimal NewQuantity { get; set; }
        public string? Reason { get; set; }
    }
}
