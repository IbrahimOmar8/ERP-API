using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.Loyalty
{
    public class Coupon
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Description { get; set; }

        public DiscountType Type { get; set; } = DiscountType.Percentage;

        // For Percentage: 0..100. For FixedAmount: EGP value.
        public decimal Value { get; set; }

        // Optional minimum invoice subtotal required to apply
        public decimal MinSubtotal { get; set; }

        // Optional cap on the discount amount (for percentage type)
        public decimal? MaxDiscountAmount { get; set; }

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }

        // Total times the coupon may be used across all customers (null = unlimited)
        public int? MaxUses { get; set; }

        // Times any one customer may use it (null = unlimited)
        public int? MaxUsesPerCustomer { get; set; }

        public int UsageCount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
