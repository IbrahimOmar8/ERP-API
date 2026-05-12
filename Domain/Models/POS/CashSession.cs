using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.POS
{
    public class CashSession
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CashRegisterId { get; set; }
        public CashRegister? CashRegister { get; set; }

        public Guid CashierUserId { get; set; }

        public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal ExpectedBalance { get; set; }
        public decimal Difference { get; set; }

        public decimal TotalCashSales { get; set; }
        public decimal TotalCardSales { get; set; }
        public decimal TotalOtherSales { get; set; }
        public decimal TotalRefunds { get; set; }

        public CashSessionStatus Status { get; set; } = CashSessionStatus.Open;

        [StringLength(500)]
        public string? Notes { get; set; }

        public ICollection<Sale>? Sales { get; set; }
    }
}
