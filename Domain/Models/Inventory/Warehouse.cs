using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Inventory
{
    public class Warehouse
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(150)]
        public string NameAr { get; set; } = string.Empty;

        [StringLength(150)]
        public string? NameEn { get; set; }

        [Required, StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        public Guid? ManagerEmployeeId { get; set; }

        public bool IsMain { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<StockItem>? StockItems { get; set; }
    }
}
