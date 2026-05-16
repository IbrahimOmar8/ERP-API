using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.Cheques
{
    // Tracks Egyptian post-dated cheques in both directions.
    // Incoming = customer paid us with a cheque.
    // Outgoing = we issued a cheque to a supplier.
    public class Cheque
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Printed cheque number on the paper
        [Required, StringLength(50)]
        public string ChequeNumber { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string BankName { get; set; } = string.Empty;

        [StringLength(150)]
        public string? BranchName { get; set; }

        // Name on the cheque — for incoming this is the drawer
        [StringLength(150)]
        public string? AccountHolderName { get; set; }

        public decimal Amount { get; set; }

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        // Cheque maturity date — the most important field for follow-up
        public DateTime DueDate { get; set; }

        public ChequeType Type { get; set; }

        public ChequeStatus Status { get; set; } = ChequeStatus.Pending;

        // Counterparty references — exactly one is set based on Type
        public Guid? CustomerId { get; set; }
        public Guid? SupplierId { get; set; }

        // Optional links to the originating document (sale or purchase)
        public Guid? SaleId { get; set; }
        public Guid? PurchaseInvoiceId { get; set; }

        // Status timestamps
        public DateTime? DepositedAt { get; set; }
        public DateTime? ClearedAt { get; set; }
        public DateTime? BouncedAt { get; set; }

        [StringLength(500)]
        public string? BounceReason { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Once cleared we post a CustomerPayment/SupplierPayment — recorded here for traceability
        public Guid? LinkedPaymentId { get; set; }

        public Guid? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
