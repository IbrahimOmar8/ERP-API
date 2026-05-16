using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.Delivery
{
    // Tracks a single delivery from creation to hand-off, including
    // cash-on-delivery reconciliation with the driver.
    public class DeliveryOrder
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Human-readable sequential number (DLV-0001)
        [Required, StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        // Optional link to the originating Sale (when delivery is for a POS sale)
        public Guid? SaleId { get; set; }

        public Guid? CustomerId { get; set; }

        [StringLength(150)]
        public string? CustomerName { get; set; }

        [StringLength(50)]
        public string? CustomerPhone { get; set; }

        [Required, StringLength(500)]
        public string Address { get; set; } = string.Empty;

        public Guid? ZoneId { get; set; }

        public decimal DeliveryFee { get; set; }

        // Total amount the driver must collect from the customer
        // (0 when the sale is prepaid, equals sale total + fee for COD)
        public decimal CashToCollect { get; set; }

        // What the driver actually handed back to the cashier
        public decimal CashCollected { get; set; }

        public Guid? DriverId { get; set; }

        public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;

        public DateTime? AssignedAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public Guid? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
