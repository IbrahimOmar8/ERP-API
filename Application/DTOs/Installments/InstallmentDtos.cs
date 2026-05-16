using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Installments
{
    public class InstallmentPlanDto
    {
        public Guid Id { get; set; }
        public string PlanNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public Guid? SaleId { get; set; }
        public string? SaleNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DownPayment { get; set; }
        public decimal FinancedAmount { get; set; }
        public int InstallmentCount { get; set; }
        public decimal InstallmentAmount { get; set; }
        public InstallmentFrequency Frequency { get; set; }
        public DateTime StartDate { get; set; }
        public InstallmentPlanStatus Status { get; set; }
        public string? Notes { get; set; }

        // Live aggregates
        public decimal TotalPaid { get; set; }
        public decimal Remaining { get; set; }
        public int PaidCount { get; set; }
        public int OverdueCount { get; set; }
        public DateTime? NextDueDate { get; set; }
        public decimal? NextDueAmount { get; set; }

        public List<InstallmentDto> Installments { get; set; } = new();
    }

    public class InstallmentDto
    {
        public Guid Id { get; set; }
        public int Sequence { get; set; }
        public DateTime DueDate { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountPaid { get; set; }
        public InstallmentStatus Status { get; set; }
        public DateTime? PaidAt { get; set; }
        public int DaysOverdue { get; set; }
    }

    public class CreateInstallmentPlanDto
    {
        [Required] public Guid CustomerId { get; set; }
        public Guid? SaleId { get; set; }

        [Range(0.01, double.MaxValue)] public decimal TotalAmount { get; set; }
        public decimal DownPayment { get; set; }

        [Range(1, 60)] public int InstallmentCount { get; set; } = 1;
        public InstallmentFrequency Frequency { get; set; } = InstallmentFrequency.Monthly;
        public DateTime? StartDate { get; set; }

        [StringLength(500)] public string? Notes { get; set; }
    }

    public class PayInstallmentDto
    {
        // Optional override — defaults to the scheduled amount
        public decimal? Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        [StringLength(100)] public string? Reference { get; set; }
    }
}
