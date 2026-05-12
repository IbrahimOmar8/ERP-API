using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Inventory
{
    public class Category
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(150)]
        public string NameAr { get; set; } = string.Empty;

        [StringLength(150)]
        public string? NameEn { get; set; }

        public Guid? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }

        public ICollection<Category>? SubCategories { get; set; }
        public ICollection<Product>? Products { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
