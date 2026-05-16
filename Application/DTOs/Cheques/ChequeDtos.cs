using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Cheques
{
    public class ChequeDto
    {
        public Guid Id { get; set; }
        public string ChequeNumber { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string? BranchName { get; set; }
        public string? AccountHolderName { get; set; }
        public decimal Amount { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public ChequeType Type { get; set; }
        public ChequeStatus Status { get; set; }

        public Guid? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SupplierName { get; set; }

        public Guid? SaleId { get; set; }
        public string? SaleNumber { get; set; }
        public Guid? PurchaseInvoiceId { get; set; }
        public string? PurchaseNumber { get; set; }

        public DateTime? DepositedAt { get; set; }
        public DateTime? ClearedAt { get; set; }
        public DateTime? BouncedAt { get; set; }
        public string? BounceReason { get; set; }
        public string? Notes { get; set; }
        public Guid? LinkedPaymentId { get; set; }

        public int DaysToDue { get; set; } // negative = overdue
    }

    public class CreateChequeDto
    {
        [Required, StringLength(50)] public string ChequeNumber { get; set; } = string.Empty;
        [Required, StringLength(150)] public string BankName { get; set; } = string.Empty;
        [StringLength(150)] public string? BranchName { get; set; }
        [StringLength(150)] public string? AccountHolderName { get; set; }
        [Range(0.01, double.MaxValue)] public decimal Amount { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;
        [Required] public DateTime DueDate { get; set; }
        [Required] public ChequeType Type { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? SupplierId { get; set; }
        public Guid? SaleId { get; set; }
        public Guid? PurchaseInvoiceId { get; set; }
        [StringLength(500)] public string? Notes { get; set; }
    }

    public class ChequeFilterDto
    {
        public ChequeType? Type { get; set; }
        public ChequeStatus? Status { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? SupplierId { get; set; }
        public DateTime? DueFrom { get; set; }
        public DateTime? DueTo { get; set; }
    }

    public class BounceChequeDto
    {
        [StringLength(500)] public string? Reason { get; set; }
    }

    public class ChequeStatsDto
    {
        public int IncomingPending { get; set; }
        public decimal IncomingPendingAmount { get; set; }
        public int OutgoingPending { get; set; }
        public decimal OutgoingPendingAmount { get; set; }
        public int DueSoon { get; set; }         // due within 7 days
        public int Overdue { get; set; }         // due date passed, still pending/deposited
        public int BouncedThisMonth { get; set; }
    }
}
