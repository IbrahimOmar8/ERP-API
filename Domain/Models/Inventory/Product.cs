using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Inventory
{
    public class Product
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(50)]
        public string Sku { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Barcode { get; set; }

        [Required, StringLength(250)]
        public string NameAr { get; set; } = string.Empty;

        [StringLength(250)]
        public string? NameEn { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public Guid CategoryId { get; set; }
        public Category? Category { get; set; }

        public Guid UnitId { get; set; }
        public Unit? Unit { get; set; }

        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinSalePrice { get; set; }

        // Egyptian VAT - default 14%
        public decimal VatRate { get; set; } = 14m;

        // ETA classification codes for Egyptian e-invoicing
        [StringLength(50)]
        public string? ItemCode { get; set; }

        [StringLength(50)]
        public string? GS1Code { get; set; }

        public decimal MinStockLevel { get; set; }
        public decimal MaxStockLevel { get; set; }

        public bool TrackStock { get; set; } = true;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<StockItem>? StockItems { get; set; }
    }
}
