using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Inventory
{
    public class Supplier
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

        // Egyptian tax registration number
        [StringLength(50)]
        public string? TaxRegistrationNumber { get; set; }

        // Egyptian commercial registration
        [StringLength(50)]
        public string? CommercialRegister { get; set; }

        public decimal Balance { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<PurchaseInvoice>? PurchaseInvoices { get; set; }
    }
}
