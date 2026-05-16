using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.HR
{
    // Single employee payslip for a given month.
    public class Payroll
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid EmployeeId { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }

        // Snapshots so payslip is reproducible
        public decimal BaseSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }

        public decimal OvertimePay { get; set; }
        public decimal LatePenalty { get; set; }
        public decimal UnpaidLeavePenalty { get; set; }

        // Installment amount auto-deducted from any active EmployeeLoan
        public decimal LoanDeduction { get; set; }

        public decimal Bonus { get; set; }
        public decimal Tax { get; set; }
        public decimal InsuranceContribution { get; set; }

        public int WorkingDays { get; set; }
        public int AbsentDays { get; set; }
        public decimal OvertimeHours { get; set; }
        public int LateMinutes { get; set; }

        public decimal GrossPay { get; set; }
        public decimal NetPay { get; set; }

        public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

        [StringLength(500)]
        public string? Notes { get; set; }

        public Guid? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
