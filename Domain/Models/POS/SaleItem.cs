using System.ComponentModel.DataAnnotations;
using Domain.Models.Inventory;

namespace Domain.Models.POS
{
    public class SaleItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SaleId { get; set; }
        public Sale? Sale { get; set; }

        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        [StringLength(250)]
        public string ProductNameSnapshot { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }

        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }

        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }

        public decimal LineSubTotal { get; set; }
        public decimal LineTotal { get; set; }
    }
}
