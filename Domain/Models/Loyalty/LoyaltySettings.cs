using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Loyalty
{
    // Singleton row holding the program configuration.
    public class LoyaltySettings
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool Enabled { get; set; }

        // How many EGP one earned point represents at redemption time.
        public decimal PointValueEgp { get; set; } = 0.10m;

        // EGP spent per point earned (e.g. 10 means: every 10 EGP = 1 point)
        public decimal EgpPerPointEarned { get; set; } = 10m;

        // Minimum points required before customer can redeem
        public int MinRedeemPoints { get; set; } = 50;

        // Max % of invoice that can be paid with points (0..100)
        public decimal MaxRedeemPercent { get; set; } = 50m;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
