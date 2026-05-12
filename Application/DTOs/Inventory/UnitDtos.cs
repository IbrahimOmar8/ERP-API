using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    public class UnitDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string Code { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateUnitDto
    {
        [Required, StringLength(50)]
        public string NameAr { get; set; } = string.Empty;
        [StringLength(50)]
        public string? NameEn { get; set; }
        [Required, StringLength(10)]
        public string Code { get; set; } = string.Empty;
    }
}
