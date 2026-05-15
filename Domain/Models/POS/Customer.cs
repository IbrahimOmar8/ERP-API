using System.ComponentModel.DataAnnotations;

namespace Domain.Models.POS
{
    public class Customer
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(250)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        // Egyptian tax registration (for B2B e-invoicing)
        [StringLength(50)]
        public string? TaxRegistrationNumber { get; set; }

        // National ID for B2C
        [StringLength(20)]
        public string? NationalId { get; set; }

        public bool IsCompany { get; set; }
        public decimal Balance { get; set; }
        public decimal CreditLimit { get; set; }

        // Loyalty
        public int LoyaltyPoints { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Sale>? Sales { get; set; }
    }
}
