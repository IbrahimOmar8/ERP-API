using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public string? ParentName { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateCategoryDto
    {
        [Required, StringLength(150)]
        public string NameAr { get; set; } = string.Empty;
        [StringLength(150)]
        public string? NameEn { get; set; }
        public Guid? ParentCategoryId { get; set; }
    }

    public class UpdateCategoryDto : CreateCategoryDto
    {
        public bool IsActive { get; set; } = true;
    }
}
