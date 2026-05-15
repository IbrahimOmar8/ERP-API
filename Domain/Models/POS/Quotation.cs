using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Domain.Models.Inventory;

namespace Domain.Models.POS
{
    public class Quotation
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(50)]
        public string QuotationNumber { get; set; } = string.Empty;

        public Guid? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // For walk-ins: store name/phone without creating a Customer
        [StringLength(150)]
        public string? CustomerNameSnapshot { get; set; }
        [StringLength(50)]
        public string? CustomerPhoneSnapshot { get; set; }

        public Guid? WarehouseId { get; set; }

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime? ValidUntil { get; set; }

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }

        public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

        // Set when this quotation was turned into a sale
        public Guid? ConvertedSaleId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Common: payment terms, validity terms, etc.
        [StringLength(1000)]
        public string? Terms { get; set; }

        public Guid? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<QuotationItem>? Items { get; set; }
    }

    public class QuotationItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid QuotationId { get; set; }
        public Quotation? Quotation { get; set; }

        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        [StringLength(250)]
        public string ProductNameSnapshot { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal LineSubTotal { get; set; }
        public decimal LineTotal { get; set; }
    }
}
