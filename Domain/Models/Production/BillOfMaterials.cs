using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Production
{
    // Recipe / bill of materials for a manufactured product.
    public class BillOfMaterials
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Finished product the BOM produces
        public Guid ProductId { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        // How many units of the finished product one batch yields
        public decimal OutputQuantity { get; set; } = 1m;

        // Optional labour/overhead added to the cost of one finished unit
        public decimal AdditionalCostPerUnit { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Notes { get; set; }

        public List<BomComponent> Components { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class BomComponent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid BillOfMaterialsId { get; set; }
        public BillOfMaterials? BillOfMaterials { get; set; }

        public Guid ProductId { get; set; }

        // Quantity of this component per single batch of the BOM
        public decimal Quantity { get; set; }

        // Permitted waste percentage (e.g. 5 means 5% waste — extra is consumed)
        public decimal WastePercent { get; set; }
    }
}
