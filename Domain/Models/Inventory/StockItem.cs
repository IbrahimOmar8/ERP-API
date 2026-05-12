using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Inventory
{
    public class StockItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        public Guid WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }

        public decimal AverageCost { get; set; }

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
