using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.POS
{
    public class HeldOrderSummaryDto
    {
        public Guid Id { get; set; }
        public string? Label { get; set; }
        public Guid CashierUserId { get; set; }
        public Guid? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalEstimate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class HeldOrderDetailDto : HeldOrderSummaryDto
    {
        public List<HeldOrderItem> Items { get; set; } = new();
    }

    public class HeldOrderItem
    {
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
    }

    public class CreateHeldOrderDto
    {
        [StringLength(100)] public string? Label { get; set; }
        public Guid? CustomerId { get; set; }
        [Required] public Guid CashSessionId { get; set; }
        [StringLength(500)] public string? Notes { get; set; }
        [Required, MinLength(1)] public List<HeldOrderItem> Items { get; set; } = new();
    }
}
