using System.ComponentModel.DataAnnotations;

namespace Domain.Models.HR
{
    public class Position
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        // Suggested base salary for this position — copied to Employee on hire
        public decimal BaseSalary { get; set; }

        public Guid? DepartmentId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
