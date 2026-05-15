using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models
{
    public class Employee
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        // National ID — required for payroll/insurance in EG
        [StringLength(20)]
        public string? NationalId { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(500)]
        public string? PhotoUrl { get; set; }

        public DateTime HireDate { get; set; } = DateTime.UtcNow;
        public DateTime? TerminationDate { get; set; }

        public EmpStatus Status { get; set; } = EmpStatus.Active;

        public Guid DepartmentId { get; set; }
        public Department? Department { get; set; }

        public Guid? PositionId { get; set; }

        // Salary structure — overrides position's BaseSalary when set
        public decimal BaseSalary { get; set; }

        // Daily allowances (transport, meal, ...) added to monthly salary
        public decimal Allowances { get; set; }

        // Fixed monthly deductions (insurance, loan installments)
        public decimal Deductions { get; set; }

        // Hourly rate for overtime — falls back to BaseSalary/working hours if 0
        public decimal OvertimeHourlyRate { get; set; }

        [StringLength(100)]
        public string? BankName { get; set; }

        [StringLength(50)]
        public string? BankAccount { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
