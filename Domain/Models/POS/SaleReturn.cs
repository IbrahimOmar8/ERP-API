using System.ComponentModel.DataAnnotations;
using Domain.Models.Inventory;

namespace Domain.Models.POS
{
    public class SaleReturn
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(50)]
        public string ReturnNumber { get; set; } = string.Empty;

        public Guid OriginalSaleId { get; set; }
        public Sale? OriginalSale { get; set; }

        public Guid? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public Guid CashSessionId { get; set; }
        public CashSession? CashSession { get; set; }

        public DateTime ReturnDate { get; set; } = DateTime.UtcNow;

        public decimal SubTotal { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        public Guid? ProcessedByUserId { get; set; }

        public ICollection<SaleReturnItem>? Items { get; set; }
    }

    public class SaleReturnItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SaleReturnId { get; set; }
        public SaleReturn? SaleReturn { get; set; }

        public Guid ProductId { get; set; }
        public Product? Product { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal VatAmount { get; set; }
        public decimal LineTotal { get; set; }
    }
}
