namespace Application.DTOs.Egypt
{
    public class CompanyProfileDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string TaxRegistrationNumber { get; set; } = string.Empty;
        public string? CommercialRegister { get; set; }
        public string? ActivityCode { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? Governorate { get; set; }
        public string? City { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? EtaClientId { get; set; }
        // Secret is write-only: never returned in GET responses
        public bool HasEtaSecret { get; set; }
        public string? EtaIssuerId { get; set; }
        public bool EtaEnabled { get; set; }
    }

    public class UpdateCompanyProfileDto
    {
        public string NameAr { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string TaxRegistrationNumber { get; set; } = string.Empty;
        public string? CommercialRegister { get; set; }
        public string? ActivityCode { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? Governorate { get; set; }
        public string? City { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? EtaClientId { get; set; }
        // Only set when non-empty; leave null/empty to keep current secret
        public string? EtaClientSecret { get; set; }
        public string? EtaIssuerId { get; set; }
        public bool EtaEnabled { get; set; }
    }
}
