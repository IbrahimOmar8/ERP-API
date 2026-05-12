using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Egypt
{
    public class TaxRate
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(150)]
        public string NameAr { get; set; } = string.Empty;

        [StringLength(150)]
        public string? NameEn { get; set; }

        [Required, StringLength(10)]
        public string Code { get; set; } = string.Empty;

        public decimal Rate { get; set; }

        // T1 (VAT 14%), T2 (Schedule), T3 (Export 0%) ...
        [StringLength(20)]
        public string? EtaTaxType { get; set; }

        [StringLength(20)]
        public string? EtaTaxSubType { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
