using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.POS
{
    public class CustomerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxRegistrationNumber { get; set; }
        public string? NationalId { get; set; }
        public bool IsCompany { get; set; }
        public decimal Balance { get; set; }
        public decimal CreditLimit { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateCustomerDto
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
        [StringLength(20)]
        public string? NationalId { get; set; }
        public bool IsCompany { get; set; }
        public decimal CreditLimit { get; set; }
    }
}
