using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Domain.Models.Inventory;

namespace Domain.Models.POS
{
    public class Sale
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public Guid? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public Guid WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public Guid CashSessionId { get; set; }
        public CashSession? CashSession { get; set; }

        public Guid CashierUserId { get; set; }

        public DateTime SaleDate { get; set; } = DateTime.UtcNow;

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }

        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }

        public SaleStatus Status { get; set; } = SaleStatus.Completed;

        // Egyptian e-invoicing reference
        [StringLength(100)]
        public string? EInvoiceUuid { get; set; }
        public EInvoiceStatus? EInvoiceStatus { get; set; }
        [StringLength(100)]
        public string? EInvoiceLongId { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<SaleItem>? Items { get; set; }
        public ICollection<SalePayment>? Payments { get; set; }
    }
}
