using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.Production
{
    // A request to manufacture a quantity of a finished product using its BOM.
    public class ProductionOrder
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Human-readable sequential number (PRD-0001)
        [Required, StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        public Guid BillOfMaterialsId { get; set; }
        public BillOfMaterials? BillOfMaterials { get; set; }

        public Guid ProductId { get; set; }
        public Guid WarehouseId { get; set; }

        // How many finished units to produce
        public decimal Quantity { get; set; }

        // Cost recorded at completion: sum(component.quantity * unit cost) + additional
        public decimal TotalCost { get; set; }

        // Cost per finished unit (TotalCost / Quantity)
        public decimal UnitCost { get; set; }

        public ProductionOrderStatus Status { get; set; } = ProductionOrderStatus.Draft;

        public DateTime PlannedDate { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public Guid? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ProductionOrderItem> Items { get; set; } = new();
    }

    // Snapshot of the components consumed when the order was completed.
    public class ProductionOrderItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProductionOrderId { get; set; }
        public ProductionOrder? ProductionOrder { get; set; }

        public Guid ProductId { get; set; }

        // Planned quantity (= bom quantity * order qty, with waste factor)
        public decimal Quantity { get; set; }

        // Cost at the time of consumption
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }
}
