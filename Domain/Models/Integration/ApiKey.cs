using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Integration
{
    public class ApiKey
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        // First 8 chars of the raw key — visible to admin for identification
        [Required, StringLength(20)]
        public string Prefix { get; set; } = string.Empty;

        // SHA-256 hex of the full raw key. We never store the raw key.
        [Required, StringLength(100)]
        public string KeyHash { get; set; } = string.Empty;

        // Comma-separated scopes (e.g. "read:products,write:sales")
        [StringLength(500)]
        public string? Scopes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public string? LastUsedIp { get; set; }

        public bool IsActive { get; set; } = true;

        public Guid? CreatedByUserId { get; set; }
    }
}
