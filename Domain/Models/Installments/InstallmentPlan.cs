using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.Installments
{
    // A customer installment plan (بيع بالتقسيط) — can be linked to a sale or stand alone.
    public class InstallmentPlan
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(50)]
        public string PlanNumber { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        // Optional link to the originating sale
        public Guid? SaleId { get; set; }

        // Headline figures captured at plan creation
        public decimal TotalAmount { get; set; }
        public decimal DownPayment { get; set; }

        // Computed: total - down. Each installment = financed / count.
        public decimal FinancedAmount { get; set; }

        public int InstallmentCount { get; set; }
        public decimal InstallmentAmount { get; set; }

        public InstallmentFrequency Frequency { get; set; } = InstallmentFrequency.Monthly;

        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

        public InstallmentPlanStatus Status { get; set; } = InstallmentPlanStatus.Active;

        [StringLength(500)]
        public string? Notes { get; set; }

        public Guid? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Installment> Installments { get; set; } = new();
    }

    public class Installment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid PlanId { get; set; }
        public InstallmentPlan? Plan { get; set; }

        public int Sequence { get; set; }

        public DateTime DueDate { get; set; }

        public decimal Amount { get; set; }

        public decimal AmountPaid { get; set; }

        public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;

        public DateTime? PaidAt { get; set; }

        // Linked CustomerPayment that recorded the payment for ledger continuity
        public Guid? LinkedPaymentId { get; set; }
    }
}
