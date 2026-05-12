using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    public class WarehouseDto
    {
        public Guid Id { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public bool IsMain { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateWarehouseDto
    {
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
    }
}
