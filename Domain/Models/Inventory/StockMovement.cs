using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.Inventory
{
    public class StockMovement
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        public Guid WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public MovementType Type { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost => Quantity * UnitCost;

        public decimal BalanceAfter { get; set; }

        // Reference document Id (Sale, Purchase, Transfer...)
        public Guid? ReferenceId { get; set; }

        [StringLength(50)]
        public string? ReferenceType { get; set; }

        [StringLength(50)]
        public string? DocumentNumber { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public Guid? CreatedByUserId { get; set; }
        public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    }
}
