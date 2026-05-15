using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Integration
{
    public class WebhookDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Events { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        // Secret is write-only; only HasSecret is returned
        public bool HasSecret { get; set; }
    }

    public class CreateWebhookDto
    {
        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, Url, StringLength(500)]
        public string Url { get; set; } = string.Empty;

        [Required, StringLength(500)]
        public string Events { get; set; } = string.Empty;

        // Optional: provide a secret, or one is generated.
        [StringLength(100)]
        public string? Secret { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class WebhookDeliveryDto
    {
        public Guid Id { get; set; }
        public Guid SubscriptionId { get; set; }
        public string Event { get; set; } = string.Empty;
        public int? ResponseStatus { get; set; }
        public string? Error { get; set; }
        public int Attempts { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
    }

    // Known event names (kept in code so we can list/document them)
    public static class WebhookEvents
    {
        public const string SaleCreated = "sale.created";
        public const string SaleRefunded = "sale.refunded";
        public const string StockLow = "stock.low";
        public const string EtaSubmitted = "eta.submitted";
        public const string EtaFailed = "eta.failed";
        public const string PurchaseCreated = "purchase.created";

        public static readonly string[] All =
        {
            SaleCreated, SaleRefunded, StockLow,
            EtaSubmitted, EtaFailed, PurchaseCreated,
        };
    }
}
