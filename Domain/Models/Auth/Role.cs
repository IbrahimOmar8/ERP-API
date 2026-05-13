using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Auth
{
    public class Role
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<UserRole>? UserRoles { get; set; }
    }
}
