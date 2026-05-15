using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.Loyalty
{
    public class LoyaltyTransaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CustomerId { get; set; }

        public LoyaltyTxType Type { get; set; }

        // Positive for earn/adjust+, negative for redeem/expire/adjust-
        public int Points { get; set; }

        public int BalanceAfter { get; set; }

        public Guid? SaleId { get; set; }

        [StringLength(250)]
        public string? Notes { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
