using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.POS
{
    public class SaleDto
    {
        public Guid Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public Guid? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public Guid CashSessionId { get; set; }
        public Guid CashierUserId { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public SaleStatus Status { get; set; }
        public string? EInvoiceUuid { get; set; }
        public EInvoiceStatus? EInvoiceStatus { get; set; }
        public string? Notes { get; set; }
        public string? CouponCode { get; set; }
        public decimal CouponDiscount { get; set; }
        public int PointsEarned { get; set; }
        public int PointsRedeemed { get; set; }
        public decimal PointsValueApplied { get; set; }
        public List<SaleItemDto> Items { get; set; } = new();
        public List<SalePaymentDto> Payments { get; set; } = new();
    }

    public class SaleItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
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

    public class SalePaymentDto
    {
        public Guid Id { get; set; }
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public DateTime PaidAt { get; set; }
    }

    public class CreateSaleDto
    {
        public Guid? CustomerId { get; set; }
        [Required]
        public Guid WarehouseId { get; set; }
        [Required]
        public Guid CashSessionId { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        [StringLength(500)]
        public string? Notes { get; set; }

        // Optional coupon code to apply at checkout (validated server-side)
        [StringLength(50)]
        public string? CouponCode { get; set; }

        // Loyalty points the customer wants to redeem against this invoice
        public int PointsToRedeem { get; set; }

        [Required, MinLength(1)]
        public List<CreateSaleItemDto> Items { get; set; } = new();
        [Required, MinLength(1)]
        public List<CreateSalePaymentDto> Payments { get; set; } = new();
    }

    public class CreateSaleItemDto
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    public class CreateSalePaymentDto
    {
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
    }

    public class SaleFilterDto
    {
        public string? Search { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? CashierUserId { get; set; }
        public Guid? WarehouseId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public SaleStatus? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
