using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Inventory
{
    public class Unit
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(50)]
        public string NameAr { get; set; } = string.Empty;

        [StringLength(50)]
        public string? NameEn { get; set; }

        [Required, StringLength(10)]
        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
