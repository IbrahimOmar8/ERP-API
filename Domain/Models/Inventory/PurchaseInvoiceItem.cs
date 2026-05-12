using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Inventory
{
    public class PurchaseInvoiceItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PurchaseInvoiceId { get; set; }
        public PurchaseInvoice? PurchaseInvoice { get; set; }

        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public decimal LineTotal { get; set; }
    }
}
