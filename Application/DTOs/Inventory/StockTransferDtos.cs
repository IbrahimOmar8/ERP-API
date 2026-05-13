namespace Application.DTOs.Inventory
{
    public class StockTransferDto
    {
        public Guid Id { get; set; }
        public string TransferNumber { get; set; } = string.Empty;
        public Guid FromWarehouseId { get; set; }
        public string FromWarehouseName { get; set; } = string.Empty;
        public Guid ToWarehouseId { get; set; }
        public string ToWarehouseName { get; set; } = string.Empty;
        public DateTime TransferDate { get; set; }
        public bool IsCompleted { get; set; }
        public string? Notes { get; set; }
        public List<StockTransferItemDto> Items { get; set; } = new();
        public decimal TotalQuantity => Items.Sum(i => i.Quantity);
        public decimal TotalValue => Items.Sum(i => i.Quantity * i.UnitCost);
    }

    public class StockTransferItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Sku { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }

    public class CreateStockTransferDto
    {
        public Guid FromWarehouseId { get; set; }
        public Guid ToWarehouseId { get; set; }
        public string? Notes { get; set; }
        public List<CreateStockTransferItemDto> Items { get; set; } = new();
    }

    public class CreateStockTransferItemDto
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
    }
}
