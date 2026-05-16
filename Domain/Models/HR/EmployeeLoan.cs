using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.HR
{
    // Salary advance (سلفة) granted to an employee, repaid via monthly payroll deductions.
    public class EmployeeLoan
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid EmployeeId { get; set; }

        public decimal Amount { get; set; }

        // Number of months across which the loan is repaid (1..N)
        public int Installments { get; set; } = 1;

        // Amount deducted from each monthly payroll
        public decimal MonthlyDeduction { get; set; }

        // Total deducted so far via payroll generation
        public decimal AmountRepaid { get; set; }

        public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime? CompletedDate { get; set; }

        public EmployeeLoanStatus Status { get; set; } = EmployeeLoanStatus.Active;

        [StringLength(500)]
        public string? Reason { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public Guid? ApprovedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
