using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.POS
{
    public class QuotationDto
    {
        public Guid Id { get; set; }
        public string QuotationNumber { get; set; } = string.Empty;
        public Guid? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerNameSnapshot { get; set; }
        public string? CustomerPhoneSnapshot { get; set; }
        public Guid? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal VatAmount { get; set; }
        public decimal Total { get; set; }
        public QuotationStatus Status { get; set; }
        public Guid? ConvertedSaleId { get; set; }
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<QuotationItemDto> Items { get; set; } = new();
    }

    public class QuotationItemDto
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

    public class CreateQuotationDto
    {
        public Guid? CustomerId { get; set; }
        [StringLength(150)] public string? CustomerNameSnapshot { get; set; }
        [StringLength(50)] public string? CustomerPhoneSnapshot { get; set; }
        public Guid? WarehouseId { get; set; }
        public DateTime? ValidUntil { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        [StringLength(500)] public string? Notes { get; set; }
        [StringLength(1000)] public string? Terms { get; set; }
        [Required, MinLength(1)] public List<CreateQuotationItemDto> Items { get; set; } = new();
    }

    public class CreateQuotationItemDto
    {
        public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    public class QuotationFilterDto
    {
        public QuotationStatus? Status { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Search { get; set; }
    }

    public class ConvertQuotationDto
    {
        [Required] public Guid CashSessionId { get; set; }
        [Required, MinLength(1)] public List<CreateSalePaymentDto> Payments { get; set; } = new();
    }
}
