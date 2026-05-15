using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Integration
{
    public class ApiKeyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string? Scopes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateApiKeyDto
    {
        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;
        // Comma-separated, e.g. "read:products,read:sales,write:sales"
        [StringLength(500)]
        public string? Scopes { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class CreatedApiKeyDto : ApiKeyDto
    {
        // The full raw key — shown only once at creation time
        public string RawKey { get; set; } = string.Empty;
    }
}
