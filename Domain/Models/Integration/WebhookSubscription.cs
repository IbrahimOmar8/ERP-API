using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Integration
{
    public class WebhookSubscription
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Url { get; set; } = string.Empty;

        // Comma-separated event names: "sale.created", "stock.low", "eta.submitted", ...
        [Required, StringLength(500)]
        public string Events { get; set; } = string.Empty;

        // HMAC-SHA256 secret used to sign payloads via X-Webhook-Signature header.
        [Required, StringLength(100)]
        public string Secret { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid? CreatedByUserId { get; set; }
    }

    public class WebhookDelivery
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SubscriptionId { get; set; }

        [Required, StringLength(100)]
        public string Event { get; set; } = string.Empty;

        [Required]
        public string Payload { get; set; } = string.Empty;

        public int? ResponseStatus { get; set; }
        public string? ResponseBody { get; set; }
        public string? Error { get; set; }
        public int Attempts { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }
    }
}
