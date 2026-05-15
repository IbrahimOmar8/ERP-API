using System.ComponentModel.DataAnnotations;

namespace Domain.Models.POS
{
    // Cart that's paused mid-sale (e.g. customer left to grab another item).
    // The items + discounts are stored as JSON so we don't reserve stock.
    public class HeldOrder
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [StringLength(100)]
        public string? Label { get; set; }

        public Guid CashierUserId { get; set; }

        public Guid? CustomerId { get; set; }

        public Guid CashSessionId { get; set; }

        // Cached for display in the list without re-computing
        public int ItemCount { get; set; }
        public decimal TotalEstimate { get; set; }

        [Required]
        public string ItemsJson { get; set; } = "[]";

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
