using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Inventory
{
    public class StockTransfer
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(50)]
        public string TransferNumber { get; set; } = string.Empty;

        public Guid FromWarehouseId { get; set; }
        public Warehouse? FromWarehouse { get; set; }

        public Guid ToWarehouseId { get; set; }
        public Warehouse? ToWarehouse { get; set; }

        public DateTime TransferDate { get; set; } = DateTime.UtcNow;
        public bool IsCompleted { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public ICollection<StockTransferItem>? Items { get; set; }
    }

    public class StockTransferItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StockTransferId { get; set; }
        public StockTransfer? StockTransfer { get; set; }

        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}
