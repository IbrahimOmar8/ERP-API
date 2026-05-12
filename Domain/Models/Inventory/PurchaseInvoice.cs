using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Inventory
{
    public class PurchaseInvoice
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public Guid SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public Guid WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining => Total - Paid;

        [StringLength(500)]
        public string? Notes { get; set; }

        public Guid? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PurchaseInvoiceItem>? Items { get; set; }
    }
}
