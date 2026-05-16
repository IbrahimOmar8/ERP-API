using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Delivery
{
    // Predefined geographic zone with a fixed fee and expected ETA.
    public class DeliveryZone
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public decimal Fee { get; set; }

        // Typical delivery time in minutes for SLA tracking
        public int EstimatedMinutes { get; set; } = 30;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
