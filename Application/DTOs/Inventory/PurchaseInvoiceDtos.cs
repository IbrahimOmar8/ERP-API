using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    public class PurchaseInvoiceDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }
        public decimal Paid { get; set; }
        public decimal Remaining { get; set; }
        public string? Notes { get; set; }
        public List<PurchaseInvoiceItemDto> Items { get; set; } = new();
    }

    public class PurchaseInvoiceItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class CreatePurchaseInvoiceDto
    {
        [Required]
        public Guid SupplierId { get; set; }
        [Required]
        public Guid WarehouseId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public decimal Paid { get; set; }
        [StringLength(500)]
        public string? Notes { get; set; }
        [Required]
        public List<CreatePurchaseInvoiceItemDto> Items { get; set; } = new();
    }

    public class CreatePurchaseInvoiceItemDto
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatRate { get; set; } = 14m;
    }
}
