using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Egypt
{
    // Holds tax registration data required for Egyptian e-invoicing (ETA)
    public class CompanyProfile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(250)]
        public string NameAr { get; set; } = string.Empty;

        [StringLength(250)]
        public string? NameEn { get; set; }

        [Required, StringLength(50)]
        public string TaxRegistrationNumber { get; set; } = string.Empty;

        [StringLength(50)]
        public string? CommercialRegister { get; set; }

        [StringLength(20)]
        public string? ActivityCode { get; set; }

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Governorate { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        // ETA portal credentials (encrypted)
        [StringLength(100)]
        public string? EtaClientId { get; set; }

        [StringLength(500)]
        public string? EtaClientSecret { get; set; }

        [StringLength(500)]
        public string? EtaIssuerId { get; set; }

        public bool EtaEnabled { get; set; }
    }
}
