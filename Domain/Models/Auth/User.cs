using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Auth
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public Guid? EmployeeId { get; set; }

        // Limit cashier to a specific warehouse/register
        public Guid? DefaultWarehouseId { get; set; }
        public Guid? DefaultCashRegisterId { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        public ICollection<UserRole>? UserRoles { get; set; }
    }
}
