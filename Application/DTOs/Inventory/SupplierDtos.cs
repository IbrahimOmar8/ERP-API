using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    public class SupplierDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxRegistrationNumber { get; set; }
        public string? CommercialRegister { get; set; }
        public decimal Balance { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSupplierDto
    {
        [Required, StringLength(250)]
        public string Name { get; set; } = string.Empty;
        [StringLength(50)]
        public string? Phone { get; set; }
        [StringLength(100)]
        public string? Email { get; set; }
        [StringLength(500)]
        public string? Address { get; set; }
        [StringLength(50)]
        public string? TaxRegistrationNumber { get; set; }
        [StringLength(50)]
        public string? CommercialRegister { get; set; }
    }
}
