using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Loyalty
{
    public class CouponDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DiscountType Type { get; set; }
        public decimal Value { get; set; }
        public decimal MinSubtotal { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public int? MaxUses { get; set; }
        public int? MaxUsesPerCustomer { get; set; }
        public int UsageCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateCouponDto
    {
        [Required, StringLength(50)]
        public string Code { get; set; } = string.Empty;
        [StringLength(250)]
        public string? Description { get; set; }
        public DiscountType Type { get; set; } = DiscountType.Percentage;
        [Range(0, double.MaxValue)]
        public decimal Value { get; set; }
        public decimal MinSubtotal { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public int? MaxUses { get; set; }
        public int? MaxUsesPerCustomer { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CouponValidationDto
    {
        public bool Valid { get; set; }
        public string? Error { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Description { get; set; }
    }

    public class ValidateCouponRequest
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public Guid? CustomerId { get; set; }
    }

    public class LoyaltySettingsDto
    {
        public bool Enabled { get; set; }
        public decimal PointValueEgp { get; set; }
        public decimal EgpPerPointEarned { get; set; }
        public int MinRedeemPoints { get; set; }
        public decimal MaxRedeemPercent { get; set; }
    }

    public class LoyaltyTransactionDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public LoyaltyTxType Type { get; set; }
        public int Points { get; set; }
        public int BalanceAfter { get; set; }
        public Guid? SaleId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CustomerLoyaltyStatusDto
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int CurrentPoints { get; set; }
        public decimal PointsValue { get; set; }
        public List<LoyaltyTransactionDto> RecentTransactions { get; set; } = new();
    }
}
